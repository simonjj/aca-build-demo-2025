var builder = DistributedApplication.CreateBuilder(args);

// ─── Step 1: Register your React app ───────────────────────────────────────────
//   - "petzoo-frontend" is just the logical name.
//   - "../frontend" points at src/frontend (relative to src/Aspire.AppHost).
builder
  .AddNpmApp("petzoo-frontend", "../frontend")
  // Expose port 3000 on your host → container port 3000
  .WithHttpEndpoint(80, env: "PORT");
// Wait until the dev server at http://localhost:{PORT} is responding

builder.Build().Run();
