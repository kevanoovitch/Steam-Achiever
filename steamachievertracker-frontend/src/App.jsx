import { useState } from 'react';

function App() {
  // state vars
  const [profileId, setProfileId] = useState('')
  const [appId, setAppId] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [result, setResult] = useState(null)


  async function handleSubmit(event)
  {
    event.preventDefault()
    
    setError(null);
    setResult(null);
    setLoading(true);

    const url = `${import.meta.env.VITE_API_BASE_URL}/api/achievements/unfinished?steamid=${encodeURIComponent(profileId)}&appid=${encodeURIComponent(appId)}`;

  try {
      
      const res = await fetch(url, { cache: 'no-store' });

      if (!res.ok) {
      // 304/404/500 etc. may have empty or non-JSON bodies
      const text = await res.text();
      throw new Error(`HTTP ${res.status} ${res.statusText}${text ? `: ${text}` : ""}`);
      }

      const data = await res.json();
      setResult(data);
    } catch (e) {
      setError(e.message || "Request failed");
    } finally {
      setLoading(false);
    }
  }

  
  return (
      <div style={{ padding: "2rem", fontFamily: "sans-serif"}}>
        <h1>Steam Achievement Tracker</h1>
        <p>Frontend connected to the C# backend API.</p>

        <form onSubmit={handleSubmit}>
          <label style={{ display: "block", marginBottom: 8}}>
            Profile ID:
            <input 
            type="text"
            value={profileId}
            onChange={(e) => setProfileId(e.target.value)}
            style={{ marginLeft: 8 }}
            required />
          </label>

          <label style={{display: "block", marginBottom:8}}>
            App ID:
            <input 
              type = "text"
              value={appId}
              onChange={(e) => setAppId(e.target.value)}
              style={{ maringLeft: 8}}
              required
              />
          </label>

          <button type="submit" disabled={!profileId || !appId || loading}>
            {loading ? "Loading..." : "Submit"}
          </button>
        </form>

        {error && ( <div style={{ marginTop: 16, color: "white", background: "crimson", padding: 8}}>
          {error}
          </div>
        )}

        {result && (
          <pre style={{ marginTop: 16, background: "#111", color: "#0f0", padding: 12, overflowX: "auto"}}>
           {JSON.stringify(result, null,2)} 
          </pre>
        )}
      </div>
    );
}
export default App;
