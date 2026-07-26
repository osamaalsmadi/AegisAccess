import { useState, useEffect } from 'react';

type DashboardData = { totalUsers: number, totalDepartments: number, totalRoles: number, totalResources: number };

function App() {
  const [persona, setPersona] = useState('securityadmin');
  const [activeTab, setActiveTab] = useState('Dashboard');
  const [data, setData] = useState<unknown>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true); setError(null); setData(null);
      try {
        const endpoint = activeTab === 'Dashboard' ? '/api/dashboard' : `/api/${activeTab.toLowerCase().replace(' ', '')}`;
        const res = await fetch(`http://localhost:5091${endpoint}`, { headers: { 'X-Demo-User': persona } });
        if (!res.ok) throw new Error(`HTTP ${res.status}: ${res.statusText}`);
        setData(await res.json());
      } catch (err: unknown) {
        if (err instanceof Error) setError(err.message);
        else setError("Unknown error occurred");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [persona, activeTab]);

  return (
    <div className="container">
      <div className="sidebar">
        <h2>AegisAccess</h2><p style={{color: '#94a3b8', fontSize: '12px'}}>Lightweight Phase 1</p>
        <div style={{marginTop: '30px'}}>
          {['Dashboard', 'Users', 'Departments', 'Roles', 'Permissions', 'Resources', 'AuditEvents'].map(tab => (
            <button key={tab} className={`nav-btn ${activeTab === tab ? 'active' : ''}`} onClick={() => setActiveTab(tab)}>{tab}</button>
          ))}
        </div>
      </div>
      <div className="main">
        <div className="warning">DEVELOPMENT DEMO AUTHENTICATION - NOT FOR PRODUCTION<br/><small>Keycloak, OPA, and Vault integrations are pending Phase 2.</small></div>
        <div className="card" style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
          <h2 style={{margin: 0}}>{activeTab}</h2>
          <div>
            <label style={{marginRight: '10px'}}>Demo Persona:</label>
            <select value={persona} onChange={e => setPersona(e.target.value)}>
              <option value="securityadmin">SecurityAdmin</option>
              <option value="employee">Employee</option>
              <option value="approver">Approver</option>
              <option value="auditor">Auditor</option>
              <option value="">No Auth Header</option>
            </select>
          </div>
        </div>
        <div className="card">
          {loading && <p>Loading data...</p>}
          {error && <p style={{color: 'red', fontWeight: 'bold'}}>{error}</p>}
          {!loading && !error && activeTab === 'Dashboard' && data && (
            <div className="grid">
              <div className="stat-box"><h4>Users</h4><p>{(data as DashboardData).totalUsers}</p></div>
              <div className="stat-box"><h4>Departments</h4><p>{(data as DashboardData).totalDepartments}</p></div>
              <div className="stat-box"><h4>Roles</h4><p>{(data as DashboardData).totalRoles}</p></div>
              <div className="stat-box"><h4>Resources</h4><p>{(data as DashboardData).totalResources}</p></div>
            </div>
          )}
          {!loading && !error && activeTab !== 'Dashboard' && data && (
            <pre style={{background: '#f1f5f9', padding: '15px', borderRadius: '4px', overflowX: 'auto'}}>
              {JSON.stringify(data, null, 2)}
            </pre>
          )}
        </div>
      </div>
    </div>
  );
}
export default App;