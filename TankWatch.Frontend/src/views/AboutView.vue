<template>
  <div class="about">
    <div class="about-header">
      <h1>About TankWatch</h1>
      <a href="https://github.com/mbyrdal" target="_blank" rel="noopener" class="github-link">
        <svg class="github-icon" viewBox="0 0 16 16" width="20" height="20" fill="currentColor">
          <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"/>
        </svg>
        Mads Byrdal Nielsen
      </a>
    </div>

    <section>
      <h2>Overview</h2>
      <p>TankWatch is a personal project that aggregates real‑time fuel prices from gas stations across Denmark. It uses publicly available APIs, which fuel retailers are legally required to provide as of 2026, to help drivers find the best prices.</p>
    </section>

    <section>
      <h2>Current coverage</h2>
      <p>At launch, TankWatch integrates data from <strong>Q8 and F24</strong> (both share the same API). Work is underway to include additional brands such as Circle K, Uno-X, and others. The goal is a comprehensive, brand‑agnostic overview of fuel prices nationwide.</p>
    </section>

    <section>
      <h2>How it works</h2>
      <p>Price data is fetched periodically from each brand’s public API and stored in a PostgreSQL database with PostGIS for efficient spatial queries. Station addresses are geocoded using OpenStreetMap’s Nominatim service, respecting their usage policy with gradual, rate‑limited updates. The frontend, built with Vue 3 and Leaflet, connects via SignalR to receive live updates.</p>
    </section>

    <section>
      <h2>Key features</h2>
      <ul>
        <li><strong>Live map:</strong> Browse stations within any radius of your location, with visual indicators for stations that have current prices.</li>
        <li><strong>Brand overview:</strong> The home page displays the latest diesel and benzin 95 prices per brand, aggregated from all reporting stations.</li>
        <li><strong>Real‑time updates:</strong> New prices appear automatically as they are fetched, without refreshing the page.</li>
        <li><strong>Responsive design:</strong> Works on desktop and mobile devices.</li>
        <li><strong>Planned expansion:</strong> Support for more fuel types (E85, HVO, etc.) and additional brands.</li>
      </ul>
    </section>

    <section>
      <h2>Technology stack</h2>
      <ul>
        <li><strong>Frontend:</strong> Vue 3, TypeScript, Pinia, Vue Router, Leaflet, SignalR client</li>
        <li><strong>Backend:</strong> .NET 10, Entity Framework Core, PostgreSQL/PostGIS, SignalR</li>
        <li><strong>Geocoding:</strong> Nominatim (OpenStreetMap)</li>
        <li><strong>Data sources:</strong> Public APIs of fuel retailers (starting with Q8/F24)</li>
      </ul>
    </section>

    <hr />

    <p class="footer">
      TankWatch is an independent project and not affiliated with any fuel brand.<br />
      Data is sourced from publicly available APIs in accordance with Danish legislation.<br />
      Currently supporting Q8 and F24; more brands coming soon.
    </p>
  </div>
</template>

<style scoped>
.about {
  max-width: 800px;
  margin: 2rem auto;
  padding: 2rem;
  background: white;
  border-radius: 16px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.08);
  line-height: 1.6;
  color: #2c3e50;
}

.about-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.about-header h1 {
  color: #42b983;
  font-size: 2rem;
  border-bottom: 2px solid #42b983;
  padding-bottom: 0.5rem;
  margin: 0;
}

.github-link {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: #2c3e50;
  text-decoration: none;
  font-weight: 500;
  padding: 0.25rem 0;
  position: relative;
  transition: color 0.2s ease;
}

.github-link::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  width: 0;
  height: 2px;
  background: #42b983;
  transition: width 0.2s ease;
}

.github-link:hover {
  color: #42b983;
}

.github-link:hover::after {
  width: 100%;
}

.github-icon {
  display: block;
  width: 20px;
  height: 20px;
  transition: transform 0.2s ease;
}

.about h2 {
  color: #2c3e50;
  font-size: 1.4rem;
  margin: 1.5rem 0 0.5rem;
}

.about ul {
  padding-left: 1.5rem;
}

.about li {
  margin: 0.5rem 0;
}

.about hr {
  border: none;
  border-top: 1px solid #e0e0e0;
  margin: 2rem 0 1rem;
}

.about .footer {
  text-align: center;
  font-size: 0.9rem;
  color: #6c757d;
}
</style>
