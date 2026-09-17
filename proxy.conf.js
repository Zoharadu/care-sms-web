const { createProxyMiddleware } = require('http-proxy-middleware');
const { environment } = require('./environments/environment');

module.exports = function(app) {
  app.use(
    '/',
    createProxyMiddleware({
      target: environment.baseUrl,
      changeOrigin: true,
      secure: false,
      logLevel: 'debug'
    })
  );
};


