console.log("SentryBlazor: Running SentryBlazor.razor.js");

window.sentryBlazor = {
  initSentryJavaScript: function (configure) {
    if (Sentry.init !== undefined) {
      console.log("SentryBlazor: calling Sentry.init");
      Sentry.init(configure);
    } else {
      console.log("SentryBlazor: Sentry.init is not available");
    }
  }
}
