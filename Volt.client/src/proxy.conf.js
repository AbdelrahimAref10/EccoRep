
const PROXY_CONFIG = [
  {
    context: ["/api"],
    target: "https://localhost:7284",
    secure: false,
    changeOrigin: true,
    logLevel: "warn",
    onError: (err, req, res) => {
      if (res.headersSent) {
        return;
      }
      res.writeHead(502, { "Content-Type": "application/json" });
      res.end(JSON.stringify({
        message: "API server unreachable. Start Volt.Server (https://localhost:7284).",
        detail: err?.message || "proxy error"
      }));
    },
  },
  {
    context: ["/uploads"],
    target: "https://localhost:7284",
    secure: false,
    changeOrigin: true,
    logLevel: "warn",
  },
  {
    context: ["/AdminNotificationHub"],
    target: "https://localhost:7284",
    secure: false,
    changeOrigin: true,
    ws: true,
    logLevel: "warn",
  },
];

module.exports = PROXY_CONFIG;
