window.sentryBlazor = {
  initSentryJavaScript: function (configure) {
    if (Sentry.init !== undefined) {
      Sentry.init(configure);
    } else {
      console.log("Sentry.init is not available");
    }
  }
}
