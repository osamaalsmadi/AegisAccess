import { FormEvent, useEffect, useState } from 'react';

type Persona = 'securityadmin' | 'employee' | 'approver' | 'auditor' | '';

type DashboardData = {
  totalUsers: number;
  totalDepartments: number;
  totalRoles: number;
  totalResources: number;
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
  accessExpiresAtUtc: string;
  status: string;
  riskScore: number;
  riskLevel: string;
  riskReasons: string[];
  recommendation: string;
  decidedByUsername: string;
  decidedAtUtc: string | null;
  decisionComment: string;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
};

const apiBase = 'http://localhost:5091';
const tabs = ['Dashboard', 'Access Requests', 'Users', 'Departments', 'Roles', 'Permissions', 'Resources', 'Audit Events'];

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

function titleForKey(key: string): string {
  return key
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, value => value.toUpperCase());
}

function GenericTable({ data }: { data: unknown }) {
  if (!Array.isArray(data) || data.length === 0) {
    return <div className="empty-state">No records were returned for this view.</div>;
  }

  const rows = data as Record<string, unknown>[];
  const columns = Object.keys(rows[0]).filter(key => !key.toLowerCase().includes('id')).slice(0, 6);

  return (
    <div className="table-shell">
      <table>
        <thead>
          <tr>{columns.map(column => <th key={column}>{titleForKey(column)}</th>)}</tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={String(row.id ?? index)}>
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
  const [data, setData] = useState<unknown>(null);
  const [catalog, setCatalog] = useState<AccessCatalog>({ resources: [], roles: [] });
  const [accessRequests, setAccessRequests] = useState<AccessRequest[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [resourceId, setResourceId] = useState('');
  const [requestedRoleId, setRequestedRoleId] = useState('');
  const [justification, setJustification] = useState('');
  const [durationDays, setDurationDays] = useState(30);

  async function requestApi<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set('Content-Type', 'application/json');
    if (persona) headers.set('X-Demo-User', persona);

    const response = await fetch(`${apiBase}${path}`, { ...init, headers });
    const contentType = response.headers.get('content-type') ?? '';
    const payload = contentType.includes('application/json')
      ? await response.json()
      : await response.text();

    if (!response.ok) {
      const problem = payload as ProblemDetails;
      throw new Error(problem.detail || problem.title || `HTTP ${response.status}`);
    }

    return payload as T;
  }

  async function loadActiveView() {
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      if (activeTab === 'Access Requests') {
        const [requests, loadedCatalog] = await Promise.all([
          requestApi<AccessRequest[]>('/api/accessrequests'),
          requestApi<AccessCatalog>('/api/accessrequests/catalog')
        ]);
        setAccessRequests(requests);
        setCatalog(loadedCatalog);
        if (!resourceId && loadedCatalog.resources.length > 0) {
          setResourceId(loadedCatalog.resources[0].id);
        }
        if (!requestedRoleId && loadedCatalog.roles.length > 0) {
          setRequestedRoleId(loadedCatalog.roles[0].id);
        }
        setData(null);
      } else {
        const endpoint = activeTab === 'Dashboard'
          ? '/api/dashboard'
          : `/api/${activeTab.toLowerCase().replace(/\s/g, '')}`;
        setData(await requestApi<unknown>(endpoint));
      }
    } catch (caught: unknown) {
      setError(caught instanceof Error ? caught.message : 'Unknown request failure.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadActiveView();
  }, [persona, activeTab, refreshKey]);

  async function submitAccessRequest(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      const created = await requestApi<AccessRequest>('/api/accessrequests', {
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
      setError(caught instanceof Error ? caught.message : 'Request creation failed.');
    }
  }

  async function decideRequest(requestId: string, decision: 'Approve' | 'Reject') {
    const comment = window.prompt(`Enter a decision comment for ${decision}:`);
    if (!comment) return;

    setError(null);
    setSuccess(null);

    try {
      const decided = await requestApi<AccessRequest>(`/api/accessrequests/${requestId}/decision`, {
        method: 'POST',
        body: JSON.stringify({ decision, comment })
      });
      setSuccess(`${decided.correlationId} is now ${decided.status}.`);
      setRefreshKey(value => value + 1);
    } catch (caught: unknown) {
      setError(caught instanceof Error ? caught.message : 'Decision failed.');
    }
  }

  const dashboard = data as DashboardData | null;
  const pendingCount = accessRequests.filter(item => item.status === 'Pending').length;
  const escalatedCount = accessRequests.filter(item => item.status === 'Escalated').length;
  const canDecide = persona === 'securityadmin' || persona === 'approver';

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">A</div>
          <div>
            <strong>AegisAccess</strong>
            <span>Identity Governance</span>
          </div>
        </div>

        <div className="environment-pill"><span /> Local Security Lab</div>

        <nav>
          {tabs.map(tab => (
            <button
              key={tab}
              className={activeTab === tab ? 'nav-item active' : 'nav-item'}
              onClick={() => setActiveTab(tab)}
            >
              <span className="nav-dot" />
              {tab}
            </button>
          ))}
        </nav>

        <div className="sidebar-footer">
          <span>ZERO TRUST FOUNDATION</span>
          <strong>Phase 2 • Governance</strong>
        </div>
      </aside>

      <main>
        <header className="topbar">
          <div>
            <p className="eyebrow">SECURITY OPERATIONS / IDENTITY</p>
            <h1>{activeTab}</h1>
          </div>
          <div className="persona-control">
            <label htmlFor="persona">Active demo identity</label>
            <select
              id="persona"
              value={persona}
              onChange={event => setPersona(event.target.value as Persona)}
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
            <strong>Development authentication is active</strong>
            <span>Persona headers simulate identities locally. Production startup remains blocked.</span>
          </div>
          <div className="shield-status">POLICY ENFORCED</div>
        </section>

        {error && <div className="alert error-alert"><strong>Request blocked</strong><span>{error}</span></div>}
        {success && <div className="alert success-alert"><strong>Workflow updated</strong><span>{success}</span></div>}
        {loading && <div className="loading-line">Evaluating identity and policy…</div>}

        {!loading && activeTab === 'Dashboard' && dashboard && (
          <>
            <section className="hero-card">
              <div>
                <p className="eyebrow">ACCESS CONTROL POSTURE</p>
                <h2>Identity decisions backed by risk context.</h2>
                <p>Track users, roles, resources, approvals, and evidence from one lightweight governance platform.</p>
              </div>
              <div className="posture-score">
                <span>FOUNDATION</span>
                <strong>92</strong>
                <small>/100</small>
              </div>
            </section>

            <section className="stats-grid">
              {[
                ['Managed identities', dashboard.totalUsers, 'Active directory records'],
                ['Departments', dashboard.totalDepartments, 'Business ownership units'],
                ['Governance roles', dashboard.totalRoles, 'RBAC policy definitions'],
                ['Protected resources', dashboard.totalResources, 'Classified enterprise assets']
              ].map(([label, value, detail]) => (
                <article className="stat-card" key={String(label)}>
                  <span>{label}</span>
                  <strong>{value}</strong>
                  <small>{detail}</small>
                </article>
              ))}
            </section>

            <section className="two-column">
              <article className="panel">
                <div className="panel-heading">
                  <div><p className="eyebrow">CONTROL COVERAGE</p><h3>Security controls</h3></div>
                  <span className="status-badge status-approved">ONLINE</span>
                </div>
                {['RBAC authorization', 'Risk-aware access requests', 'Segregation of duties', 'Immutable audit evidence'].map((control, index) => (
                  <div className="control-row" key={control}>
                    <span>{control}</span>
                    <div className="mini-track"><i style={{ width: `${88 + index * 3}%` }} /></div>
                    <strong>{88 + index * 3}%</strong>
                  </div>
                ))}
              </article>

              <article className="panel">
                <div className="panel-heading">
                  <div><p className="eyebrow">QUICK ACTION</p><h3>Govern access</h3></div>
                </div>
                <p className="muted">Create a time-limited request, calculate its risk, and route it to the correct approver.</p>
                <button className="primary-button" onClick={() => setActiveTab('Access Requests')}>
                  Open access workflow
                </button>
              </article>
            </section>
          </>
        )}

        {!loading && activeTab === 'Access Requests' && (
          <>
            <section className="workflow-summary">
              <article><span>Total visible</span><strong>{accessRequests.length}</strong></article>
              <article><span>Pending review</span><strong>{pendingCount}</strong></article>
              <article><span>Escalated</span><strong className="critical-text">{escalatedCount}</strong></article>
              <article><span>Current identity</span><strong className="identity-text">{persona || 'Anonymous'}</strong></article>
            </section>

            <section className="request-layout">
              <form className="panel request-form" onSubmit={submitAccessRequest}>
                <div className="panel-heading">
                  <div>
                    <p className="eyebrow">NEW REQUEST</p>
                    <h3>Request time-limited access</h3>
                  </div>
                  <span className="policy-chip">RISK SCORING ON</span>
                </div>

                <label>
                  Protected resource
                  <select value={resourceId} onChange={event => setResourceId(event.target.value)} required>
                    {catalog.resources.map(resource => (
                      <option key={resource.id} value={resource.id}>
                        {resource.name} • {resource.metadata}
                      </option>
                    ))}
                  </select>
                </label>

                <label>
                  Requested governance role
                  <select value={requestedRoleId} onChange={event => setRequestedRoleId(event.target.value)} required>
                    {catalog.roles.map(role => (
                      <option key={role.id} value={role.id}>
                        {role.name} • {role.metadata}
                      </option>
                    ))}
                  </select>
                </label>

                <label>
                  Access duration
                  <div className="range-row">
                    <input
                      type="range"
                      min="1"
                      max="180"
                      value={durationDays}
                      onChange={event => setDurationDays(Number(event.target.value))}
                    />
                    <strong>{durationDays} days</strong>
                  </div>
                </label>

                <label>
                  Business justification
                  <textarea
                    value={justification}
                    onChange={event => setJustification(event.target.value)}
                    minLength={15}
                    maxLength={500}
                    placeholder="Explain the business task, data required, and why current access is insufficient."
                    required
                  />
                </label>

                <button className="primary-button" type="submit" disabled={!persona}>
                  Calculate risk and submit
                </button>
              </form>

              <article className="panel policy-panel">
                <p className="eyebrow">DECISION LOGIC</p>
                <h3>Adaptive approval policy</h3>
                <div className="policy-rule"><span>01</span><div><strong>Least privilege</strong><p>Role and resource sensitivity increase the score.</p></div></div>
                <div className="policy-rule"><span>02</span><div><strong>Time limitation</strong><p>Long-lived access receives additional risk.</p></div></div>
                <div className="policy-rule"><span>03</span><div><strong>Segregation of duties</strong><p>A requester can never approve their own request.</p></div></div>
                <div className="policy-rule"><span>04</span><div><strong>Critical escalation</strong><p>Scores of 75+ require SecurityAdmin approval.</p></div></div>
              </article>
            </section>

            <section className="panel requests-panel">
              <div className="panel-heading">
                <div><p className="eyebrow">DECISION QUEUE</p><h3>Access request evidence</h3></div>
                <button className="ghost-button" onClick={() => setRefreshKey(value => value + 1)}>Refresh</button>
              </div>

              {accessRequests.length === 0 && <div className="empty-state">No access requests are visible to this identity.</div>}

              <div className="request-list">
                {accessRequests.map(request => (
                  <article className="request-card" key={request.id}>
                    <div className="request-main">
                      <div className="request-title">
                        <div>
                          <span className="correlation">{request.correlationId}</span>
                          <h4>{request.resourceName} <i>→</i> {request.requestedRoleName}</h4>
                        </div>
                        <span className={`status-badge ${statusClass(request.status)}`}>{request.status}</span>
                      </div>

                      <div className="request-meta">
                        <span><b>Requester</b>{request.requesterUsername}</span>
                        <span><b>Department</b>{request.requesterDepartment || '—'}</span>
                        <span><b>Duration</b>{request.durationDays} days</span>
                        <span><b>Submitted</b>{formatDate(request.requestedAtUtc)}</span>
                      </div>

                      <p className="justification">{request.businessJustification}</p>
                      <div className="recommendation">{request.recommendation}</div>

                      {request.riskReasons.length > 0 && (
                        <ul className="risk-reasons">
                          {request.riskReasons.map(reason => <li key={reason}>{reason}</li>)}
                        </ul>
                      )}

                      {request.decidedByUsername && (
                        <div className="decision-evidence">
                          Decision by <strong>{request.decidedByUsername}</strong> at {formatDate(request.decidedAtUtc)}
                          <span>{request.decisionComment}</span>
                        </div>
                      )}
                    </div>

                    <div className="risk-column">
                      <span className={`risk-label ${riskClass(request.riskLevel)}`}>{request.riskLevel} RISK</span>
                      <strong>{request.riskScore}</strong>
                      <small>/100</small>
                      <div className="risk-track"><i style={{ width: `${request.riskScore}%` }} /></div>

                      {canDecide && (request.status === 'Pending' || request.status === 'Escalated') && (
                        <div className="decision-actions">
                          <button onClick={() => void decideRequest(request.id, 'Approve')}>Approve</button>
                          <button className="reject-button" onClick={() => void decideRequest(request.id, 'Reject')}>Reject</button>
                        </div>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            </section>
          </>
        )}

        {!loading && activeTab !== 'Dashboard' && activeTab !== 'Access Requests' && (
          <section className="panel">
            <div className="panel-heading">
              <div><p className="eyebrow">GOVERNANCE INVENTORY</p><h3>{activeTab}</h3></div>
              <span className="policy-chip">AUTHORIZED VIEW</span>
            </div>
            <GenericTable data={data} />
          </section>
        )}
      </main>
    </div>
  );
}

export default App;
