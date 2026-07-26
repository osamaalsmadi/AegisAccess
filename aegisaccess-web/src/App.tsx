import { FormEvent, useEffect, useState } from 'react';

type Persona = 'securityadmin' | 'employee' | 'approver' | 'auditor' | '';
type UnknownRecord = Record<string, unknown>;

type GovernanceOverview = {
  postureScore: number;
  totalUsers: number;
  totalRoles: number;
  totalResources: number;
  pendingRequests: number;
  criticalRequests: number;
  activeCampaigns: number;
  pendingReviewItems: number;
  overdueReviewItems: number;
  sodViolations: number;
  riskDistribution: { level: string; count: number }[];
  recentEvents: { action: string; actor: string; target: string; timestampUtc: string }[];
};

type CatalogItem = {
  id: string;
  name: string;
  description: string;
  metadata: string;
};

type AccessCatalog = {
  resources: CatalogItem[];
  roles: CatalogItem[];
};

type AccessRequest = {
  id: string;
  correlationId: string;
  requesterUsername: string;
  requesterDepartment: string;
  resourceId: string;
  resourceName: string;
  resourceSensitivity: string;
  requestedRoleId: string;
  requestedRoleName: string;
  businessJustification: string;
  durationDays: number;
  requestedAtUtc: string;
  status: string;
  riskScore: number;
  riskLevel: string;
  riskReasons: string[];
  recommendation: string;
  decidedByUsername: string;
  decidedAtUtc: string | null;
  decisionComment: string;
};

type ReviewItem = {
  id: string;
  accessRequestId: string;
  correlationId: string;
  username: string;
  department: string;
  resourceName: string;
  resourceSensitivity: string;
  roleName: string;
  riskAtReview: number;
  decision: string;
  reviewerUsername: string;
  reviewComment: string;
  reviewedAtUtc: string | null;
};

type ReviewCampaign = {
  id: string;
  name: string;
  description: string;
  createdByUsername: string;
  createdAtUtc: string;
  dueAtUtc: string;
  completedAtUtc: string | null;
  status: string;
  totalItems: number;
  pendingItems: number;
  certifiedItems: number;
  revokedItems: number;
  escalatedItems: number;
  items: ReviewItem[];
};

type SodPolicy = {
  id: string;
  name: string;
  description: string;
  severity: string;
  firstRole: string;
  secondRole: string;
  isActive: boolean;
};

type SodViolation = {
  policyId: string;
  policyName: string;
  severity: string;
  username: string;
  department: string;
  firstRole: string;
  secondRole: string;
  evidence: string;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
};

type DecisionModal =
  | {
      kind: 'access';
      requestId: string;
      action: 'Approve' | 'Reject';
      subject: string;
    }
  | {
      kind: 'review';
      campaignId: string;
      itemId: string;
      action: 'Certify' | 'Revoke' | 'Escalate';
      subject: string;
    };

const apiBase = 'http://localhost:5091';
const primaryTabs = ['Dashboard', 'Access Requests', 'Review Campaigns', 'Policy Center'];
const inventoryTabs = ['Users', 'Departments', 'Roles', 'Permissions', 'Resources', 'Audit Events'];

function formatDate(value: string | null): string {
  if (!value) return '—';
  return new Date(value).toLocaleString();
}

function riskClass(level: string): string {
  return `risk-${level.toLowerCase()}`;
}

function statusClass(status: string): string {
  return `status-${status.toLowerCase()}`;
}

function readableKey(key: string): string {
  return key.replace(/([A-Z])/g, ' $1').replace(/^./, value => value.toUpperCase());
}

function GenericTable({ data }: { data: unknown }) {
  if (!Array.isArray(data) || data.length === 0) {
    return <div className="empty-state">No records are visible to this identity.</div>;
  }

  const rows = data as UnknownRecord[];
  const columns = Object.keys(rows[0])
    .filter(key => !key.toLowerCase().includes('id'))
    .slice(0, 7);

  return (
    <div className="table-shell">
      <table>
        <thead>
          <tr>{columns.map(column => <th key={column}>{readableKey(column)}</th>)}</tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={String(row.id ?? rowIndex)}>
              {columns.map(column => (
                <td key={column}>
                  {typeof row[column] === 'boolean'
                    ? row[column] ? 'Yes' : 'No'
                    : String(row[column] ?? '—')}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function App() {
  const [persona, setPersona] = useState<Persona>('securityadmin');
  const [activeTab, setActiveTab] = useState('Dashboard');
  const [overview, setOverview] = useState<GovernanceOverview | null>(null);
  const [genericData, setGenericData] = useState<unknown>(null);
  const [catalog, setCatalog] = useState<AccessCatalog>({ resources: [], roles: [] });
  const [requests, setRequests] = useState<AccessRequest[]>([]);
  const [campaigns, setCampaigns] = useState<ReviewCampaign[]>([]);
  const [policies, setPolicies] = useState<SodPolicy[]>([]);
  const [violations, setViolations] = useState<SodViolation[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [resourceId, setResourceId] = useState('');
  const [requestedRoleId, setRequestedRoleId] = useState('');
  const [justification, setJustification] = useState('');
  const [durationDays, setDurationDays] = useState(30);
  const [campaignName, setCampaignName] = useState('');
  const [campaignDescription, setCampaignDescription] = useState('');
  const [campaignDueDays, setCampaignDueDays] = useState(14);
  const [decisionModal, setDecisionModal] = useState<DecisionModal | null>(null);
  const [decisionComment, setDecisionComment] = useState('');

  async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    if (init.body) headers.set('Content-Type', 'application/json');
    if (persona) headers.set('X-Demo-User', persona);

    const response = await fetch(`${apiBase}${path}`, { ...init, headers });
    const contentType = response.headers.get('content-type') ?? '';

    if (!response.ok) {
      const problem = contentType.includes('application/json')
        ? await response.json() as ProblemDetails
        : { detail: await response.text() };
      throw new Error(problem.detail || problem.title || `HTTP ${response.status}`);
    }

    return (contentType.includes('application/json')
      ? await response.json()
      : await response.text()) as T;
  }

  async function loadView() {
    setLoading(true);
    setError(null);

    try {
      if (activeTab === 'Dashboard') {
        setOverview(await api<GovernanceOverview>('/api/governance/overview'));
      } else if (activeTab === 'Access Requests') {
        const [loadedRequests, loadedCatalog] = await Promise.all([
          api<AccessRequest[]>('/api/accessrequests'),
          api<AccessCatalog>('/api/accessrequests/catalog')
        ]);
        setRequests(loadedRequests);
        setCatalog(loadedCatalog);
        if (!resourceId && loadedCatalog.resources.length) setResourceId(loadedCatalog.resources[0].id);
        if (!requestedRoleId && loadedCatalog.roles.length) setRequestedRoleId(loadedCatalog.roles[0].id);
      } else if (activeTab === 'Review Campaigns') {
        setCampaigns(await api<ReviewCampaign[]>('/api/accessreviews'));
      } else if (activeTab === 'Policy Center') {
        const [loadedPolicies, loadedViolations] = await Promise.all([
          api<SodPolicy[]>('/api/governance/sod-policies'),
          api<SodViolation[]>('/api/governance/violations')
        ]);
        setPolicies(loadedPolicies);
        setViolations(loadedViolations);
      } else {
        const endpoint = `/api/${activeTab.toLowerCase().replace(/\s/g, '')}`;
        setGenericData(await api<unknown>(endpoint));
      }
    } catch (caught: unknown) {
      setError(caught instanceof Error ? caught.message : 'Request failed.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadView();
  }, [persona, activeTab, refreshKey]);

  async function createAccessRequest(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      const created = await api<AccessRequest>('/api/accessrequests', {
        method: 'POST',
        body: JSON.stringify({
          resourceId,
          requestedRoleId,
          businessJustification: justification,
          durationDays
        })
      });
      setSuccess(`${created.correlationId} created with ${created.riskLevel} risk.`);
      setJustification('');
      setRefreshKey(value => value + 1);
    } catch (caught: unknown) {
      setError(caught instanceof Error ? caught.message : 'Access request failed.');
    }
  }

  async function createCampaign(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      const created = await api<ReviewCampaign>('/api/accessreviews', {
        method: 'POST',
        body: JSON.stringify({
          name: campaignName,
          description: campaignDescription,
          dueInDays: campaignDueDays
        })
      });
      setSuccess(`${created.name} created with ${created.totalItems} review items.`);
      setCampaignName('');
      setCampaignDescription('');
      setRefreshKey(value => value + 1);
    } catch (caught: unknown) {
      setError(caught instanceof Error ? caught.message : 'Campaign creation failed.');
    }
  }

  async function submitDecision(event: FormEvent) {
    event.preventDefault();
    if (!decisionModal || decisionComment.trim().length < 5) return;
    setError(null);
    setSuccess(null);

    try {
      if (decisionModal.kind === 'access') {
        const result = await api<AccessRequest>(
          `/api/accessrequests/${decisionModal.requestId}/decision`,
          {
            method: 'POST',
            body: JSON.stringify({
              decision: decisionModal.action,
              comment: decisionComment.trim()
            })
          }
        );
        setSuccess(`${result.correlationId} is now ${result.status}.`);
      } else {
        await api<ReviewCampaign>(
          `/api/accessreviews/${decisionModal.campaignId}/items/${decisionModal.itemId}/decision`,
          {
            method: 'POST',
            body: JSON.stringify({
              decision: decisionModal.action,
              comment: decisionComment.trim()
            })
          }
        );
        setSuccess(`Review decision ${decisionModal.action} was recorded.`);
      }

      setDecisionModal(null);
      setDecisionComment('');
      setRefreshKey(value => value + 1);
    } catch (caught: unknown) {
      setError(caught instanceof Error ? caught.message : 'Decision failed.');
    }
  }

  async function exportCampaign(campaignId: string) {
    try {
      const csv = await api<string>(`/api/accessreviews/${campaignId}/export`);
      const objectUrl = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
      const link = document.createElement('a');
      link.href = objectUrl;
      link.download = `aegisaccess-review-${campaignId}.csv`;
      link.click();
      URL.revokeObjectURL(objectUrl);
    } catch (caught: unknown) {
      setError(caught instanceof Error ? caught.message : 'Campaign export failed.');
    }
  }

  const canDecide = persona === 'securityadmin' || persona === 'approver';
  const isAdmin = persona === 'securityadmin';

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">A</div>
          <div><strong>AegisAccess</strong><span>Identity Governance</span></div>
        </div>

        <div className="environment-pill"><i /> Local portfolio release</div>

        <p className="nav-label">GOVERNANCE</p>
        <nav>
          {primaryTabs.map(tab => (
            <button
              key={tab}
              className={activeTab === tab ? 'nav-item active' : 'nav-item'}
              onClick={() => { setActiveTab(tab); setSuccess(null); }}
            >
              <span className="nav-dot" />{tab}
            </button>
          ))}
        </nav>

        <p className="nav-label inventory-label">INVENTORY & EVIDENCE</p>
        <nav>
          {inventoryTabs.map(tab => (
            <button
              key={tab}
              className={activeTab === tab ? 'nav-item active' : 'nav-item'}
              onClick={() => { setActiveTab(tab); setSuccess(null); }}
            >
              <span className="nav-dot" />{tab}
            </button>
          ))}
        </nav>

        <div className="sidebar-footer">
          <span>PORTFOLIO RELEASE</span>
          <strong>v1.0 • Local IGA Platform</strong>
        </div>
      </aside>

      <main>
        <header className="topbar">
          <div>
            <p className="eyebrow">SECURITY OPERATIONS / IDENTITY GOVERNANCE</p>
            <h1>{activeTab}</h1>
          </div>
          <div className="persona-control">
            <label htmlFor="persona">Active demo identity</label>
            <select
              id="persona"
              value={persona}
              onChange={event => { setPersona(event.target.value as Persona); setSuccess(null); }}
            >
              <option value="securityadmin">SecurityAdmin</option>
              <option value="employee">Employee</option>
              <option value="approver">Approver</option>
              <option value="auditor">Auditor</option>
              <option value="">Anonymous / No Header</option>
            </select>
          </div>
        </header>

        <section className="security-banner">
          <div>
            <strong>Development authentication • Production blocked</strong>
            <span>RBAC, adaptive risk, SoD, certification reviews, audit evidence, and rate limiting are active.</span>
          </div>
          <div className="shield-status">CONTROLS ONLINE</div>
        </section>

        {error && <div className="alert error-alert"><strong>Policy blocked the action</strong><span>{error}</span></div>}
        {success && <div className="alert success-alert"><strong>Governance evidence recorded</strong><span>{success}</span></div>}
        {loading && <div className="loading-line">Evaluating identity, policies, and governance evidence…</div>}

        {!loading && !error && activeTab === 'Dashboard' && overview && (
          <>
            <section className="hero-card">
              <div>
                <p className="eyebrow">LIVE GOVERNANCE POSTURE</p>
                <h2>Every identity decision connected to real evidence.</h2>
                <p>The posture score is calculated from critical requests, SoD violations, overdue reviews, and pending access.</p>
              </div>
              <div className="posture-score">
                <span>DYNAMIC SCORE</span>
                <strong>{overview.postureScore}</strong>
                <small>/100</small>
              </div>
            </section>

            <section className="stats-grid">
              {[
                ['Managed identities', overview.totalUsers, 'Directory identities'],
                ['Protected resources', overview.totalResources, 'Classified assets'],
                ['Pending requests', overview.pendingRequests, `${overview.criticalRequests} critical`],
                ['Review campaigns', overview.activeCampaigns, `${overview.pendingReviewItems} items pending`],
                ['SoD violations', overview.sodViolations, 'Toxic combinations'],
                ['Overdue reviews', overview.overdueReviewItems, 'Certification SLA']
              ].map(([label, value, detail]) => (
                <article className="stat-card" key={String(label)}>
                  <span>{label}</span><strong>{value}</strong><small>{detail}</small>
                </article>
              ))}
            </section>

            <section className="dashboard-grid">
              <article className="panel">
                <div className="panel-heading">
                  <div><p className="eyebrow">RISK DISTRIBUTION</p><h3>Access request exposure</h3></div>
                </div>
                {overview.riskDistribution.map(bucket => {
                  const maximum = Math.max(...overview.riskDistribution.map(item => item.count), 1);
                  return (
                    <div className="risk-row" key={bucket.level}>
                      <span className={riskClass(bucket.level)}>{bucket.level}</span>
                      <div className="bar-track"><i style={{ width: `${(bucket.count / maximum) * 100}%` }} /></div>
                      <strong>{bucket.count}</strong>
                    </div>
                  );
                })}
              </article>

              <article className="panel">
                <div className="panel-heading">
                  <div><p className="eyebrow">RECENT EVIDENCE</p><h3>Governance activity</h3></div>
                </div>
                <div className="timeline">
                  {overview.recentEvents.map((event, index) => (
                    <div className="timeline-item" key={`${event.timestampUtc}-${index}`}>
                      <i />
                      <div><strong>{event.action}</strong><span>{event.actor} • {event.target}</span></div>
                      <time>{formatDate(event.timestampUtc)}</time>
                    </div>
                  ))}
                </div>
              </article>
            </section>
          </>
        )}

        {!loading && !error && activeTab === 'Access Requests' && (
          <>
            <section className="split-layout">
              <form className="panel form-panel" onSubmit={createAccessRequest}>
                <div className="panel-heading">
                  <div><p className="eyebrow">NEW REQUEST</p><h3>Request time-limited access</h3></div>
                  <span className="policy-chip">SoD + RISK ON</span>
                </div>

                <label>Protected resource
                  <select value={resourceId} onChange={event => setResourceId(event.target.value)} required>
                    {catalog.resources.map(item => (
                      <option key={item.id} value={item.id}>{item.name} • {item.metadata}</option>
                    ))}
                  </select>
                </label>
                <label>Requested role
                  <select value={requestedRoleId} onChange={event => setRequestedRoleId(event.target.value)} required>
                    {catalog.roles.map(item => (
                      <option key={item.id} value={item.id}>{item.name} • {item.metadata}</option>
                    ))}
                  </select>
                </label>
                <label>Duration: <b>{durationDays} days</b>
                  <input type="range" min="1" max="180" value={durationDays} onChange={event => setDurationDays(Number(event.target.value))} />
                </label>
                <label>Business justification
                  <textarea
                    value={justification}
                    onChange={event => setJustification(event.target.value)}
                    minLength={15}
                    maxLength={500}
                    placeholder="Describe the task, required data, and business owner."
                    required
                  />
                </label>
                <button className="primary-button" disabled={!persona}>Evaluate and submit</button>
              </form>

              <article className="panel">
                <p className="eyebrow">ADAPTIVE POLICY</p>
                <h3>Decision path</h3>
                {[
                  ['01', 'Classify resource', 'Sensitivity sets the baseline risk.'],
                  ['02', 'Evaluate privilege', 'Administrative roles increase exposure.'],
                  ['03', 'Detect toxic access', 'SoD policies detect conflicting roles.'],
                  ['04', 'Route decision', 'Critical items require SecurityAdmin.']
                ].map(([number, title, detail]) => (
                  <div className="policy-rule" key={number}>
                    <span>{number}</span><div><strong>{title}</strong><p>{detail}</p></div>
                  </div>
                ))}
              </article>
            </section>

            <section className="panel">
              <div className="panel-heading">
                <div><p className="eyebrow">DECISION QUEUE</p><h3>Access request evidence</h3></div>
                <button className="ghost-button" onClick={() => setRefreshKey(value => value + 1)}>Refresh</button>
              </div>

              <div className="request-list">
                {requests.map(request => (
                  <article className="request-card" key={request.id}>
                    <div className="request-main">
                      <div className="request-title">
                        <div><span>{request.correlationId}</span><h4>{request.resourceName} → {request.requestedRoleName}</h4></div>
                        <b className={`status-badge ${statusClass(request.status)}`}>{request.status}</b>
                      </div>
                      <div className="request-meta">
                        <span><b>Requester</b>{request.requesterUsername}</span>
                        <span><b>Department</b>{request.requesterDepartment || '—'}</span>
                        <span><b>Duration</b>{request.durationDays} days</span>
                        <span><b>Created</b>{formatDate(request.requestedAtUtc)}</span>
                      </div>
                      <p>{request.businessJustification}</p>
                      <div className="recommendation">{request.recommendation}</div>
                      <ul>{request.riskReasons.map(reason => <li key={reason}>{reason}</li>)}</ul>
                      {request.decidedByUsername && (
                        <div className="decision-proof">
                          Decision: <strong>{request.decidedByUsername}</strong> • {request.decisionComment}
                        </div>
                      )}
                    </div>
                    <aside className="risk-column">
                      <span className={riskClass(request.riskLevel)}>{request.riskLevel} RISK</span>
                      <strong>{request.riskScore}</strong><small>/100</small>
                      <div className="bar-track"><i style={{ width: `${request.riskScore}%` }} /></div>
                      {canDecide && ['Pending', 'Escalated'].includes(request.status) && (
                        <div className="decision-actions">
                          <button onClick={() => setDecisionModal({ kind: 'access', requestId: request.id, action: 'Approve', subject: request.correlationId })}>Approve</button>
                          <button className="danger-button" onClick={() => setDecisionModal({ kind: 'access', requestId: request.id, action: 'Reject', subject: request.correlationId })}>Reject</button>
                        </div>
                      )}
                    </aside>
                  </article>
                ))}
              </div>
            </section>
          </>
        )}

        {!loading && !error && activeTab === 'Review Campaigns' && (
          <>
            {isAdmin && (
              <form className="panel campaign-form" onSubmit={createCampaign}>
                <div><p className="eyebrow">NEW CERTIFICATION</p><h3>Create access review campaign</h3></div>
                <input value={campaignName} onChange={event => setCampaignName(event.target.value)} placeholder="Campaign name" minLength={5} required />
                <input value={campaignDescription} onChange={event => setCampaignDescription(event.target.value)} placeholder="Scope and review objective" minLength={10} required />
                <input type="number" min="1" max="90" value={campaignDueDays} onChange={event => setCampaignDueDays(Number(event.target.value))} />
                <button className="primary-button">Create campaign</button>
              </form>
            )}

            <div className="campaign-list">
              {campaigns.map(campaign => (
                <section className="panel campaign-card" key={campaign.id}>
                  <div className="panel-heading">
                    <div>
                      <p className="eyebrow">ACCESS CERTIFICATION</p>
                      <h3>{campaign.name}</h3>
                      <p className="muted">{campaign.description}</p>
                    </div>
                    <div className="campaign-actions">
                      <span className={`status-badge ${statusClass(campaign.status)}`}>{campaign.status}</span>
                      <button className="ghost-button" onClick={() => void exportCampaign(campaign.id)}>Export CSV</button>
                    </div>
                  </div>

                  <div className="campaign-metrics">
                    <span>Total <b>{campaign.totalItems}</b></span>
                    <span>Pending <b>{campaign.pendingItems}</b></span>
                    <span>Certified <b>{campaign.certifiedItems}</b></span>
                    <span>Revoked <b>{campaign.revokedItems}</b></span>
                    <span>Due <b>{formatDate(campaign.dueAtUtc)}</b></span>
                  </div>

                  <div className="review-items">
                    {campaign.items.map(item => (
                      <article className="review-item" key={item.id}>
                        <div>
                          <span>{item.correlationId}</span>
                          <h4>{item.username} • {item.resourceName}</h4>
                          <p>{item.roleName} • {item.department || 'No department'} • {item.resourceSensitivity}</p>
                        </div>
                        <div className="review-risk"><span className={riskClass(item.riskAtReview >= 75 ? 'Critical' : item.riskAtReview >= 55 ? 'High' : item.riskAtReview >= 30 ? 'Medium' : 'Low')}>{item.riskAtReview}/100</span></div>
                        <b className={`status-badge ${statusClass(item.decision)}`}>{item.decision}</b>
                        {canDecide && item.decision === 'Pending' && (
                          <div className="review-actions">
                            <button onClick={() => setDecisionModal({ kind: 'review', campaignId: campaign.id, itemId: item.id, action: 'Certify', subject: `${item.username} • ${item.resourceName}` })}>Certify</button>
                            <button className="danger-button" onClick={() => setDecisionModal({ kind: 'review', campaignId: campaign.id, itemId: item.id, action: 'Revoke', subject: `${item.username} • ${item.resourceName}` })}>Revoke</button>
                            <button onClick={() => setDecisionModal({ kind: 'review', campaignId: campaign.id, itemId: item.id, action: 'Escalate', subject: `${item.username} • ${item.resourceName}` })}>Escalate</button>
                          </div>
                        )}
                      </article>
                    ))}
                  </div>
                </section>
              ))}
            </div>
          </>
        )}

        {!loading && !error && activeTab === 'Policy Center' && (
          <div className="dashboard-grid">
            <section className="panel">
              <div className="panel-heading">
                <div><p className="eyebrow">SEGREGATION OF DUTIES</p><h3>Active toxic-combination policies</h3></div>
                <span className="policy-chip">{policies.length} ACTIVE</span>
              </div>
              {policies.map(policy => (
                <article className="policy-card" key={policy.id}>
                  <div><span className={`severity ${policy.severity.toLowerCase()}`}>{policy.severity}</span><h4>{policy.name}</h4></div>
                  <p>{policy.description}</p>
                  <strong>{policy.firstRole} + {policy.secondRole}</strong>
              </article>
              ))}
            </section>

            <section className="panel">
              <div className="panel-heading">
                <div><p className="eyebrow">LIVE VIOLATIONS</p><h3>Detected conflicting access</h3></div>
                <span className={violations.length ? 'critical-count' : 'healthy-count'}>{violations.length}</span>
              </div>
              {violations.length === 0 && <div className="empty-state">No active SoD violations detected.</div>}
              {violations.map((violation, index) => (
                <article className="violation-card" key={`${violation.policyId}-${violation.username}-${index}`}>
                  <span>{violation.severity}</span>
                  <h4>{violation.username} • {violation.department}</h4>
                  <p>{violation.evidence}</p>
                  <strong>{violation.policyName}</strong>
                </article>
              ))}
            </section>
          </div>
        )}

        {!loading && !error && inventoryTabs.includes(activeTab) && (
          <section className="panel">
            <div className="panel-heading">
              <div><p className="eyebrow">AUTHORIZED INVENTORY</p><h3>{activeTab}</h3></div>
              <span className="policy-chip">EVIDENCE VIEW</span>
            </div>
            <GenericTable data={genericData} />
          </section>
        )}
      </main>

      {decisionModal && (
        <div className="modal-backdrop" onMouseDown={() => setDecisionModal(null)}>
          <form className="decision-modal" onSubmit={submitDecision} onMouseDown={event => event.stopPropagation()}>
            <div className="modal-icon">A</div>
            <p className="eyebrow">GOVERNANCE DECISION</p>
            <h3>{decisionModal.action} access</h3>
            <span className="modal-subject">{decisionModal.subject}</span>
            <label>
              Decision evidence
              <textarea
                autoFocus
                minLength={5}
                maxLength={500}
                value={decisionComment}
                onChange={event => setDecisionComment(event.target.value)}
                placeholder="Document the business and security reason for this decision."
                required
              />
            </label>
            <div className="modal-actions">
              <button type="button" className="ghost-button" onClick={() => setDecisionModal(null)}>Cancel</button>
              <button className={decisionModal.action === 'Revoke' || decisionModal.action === 'Reject' ? 'primary-button destructive' : 'primary-button'}>
                Record {decisionModal.action}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

export default App;
