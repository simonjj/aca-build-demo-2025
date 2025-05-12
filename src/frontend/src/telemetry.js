// src/telemetry.js - Azure Container Apps Best Practice Implementation
import { resourceFromAttributes } from '@opentelemetry/resources';
import { diag, DiagConsoleLogger, DiagLogLevel } from '@opentelemetry/api';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor, SimpleSpanProcessor, ConsoleSpanExporter } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter} from '@opentelemetry/exporter-trace-otlp-http';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-http';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { MeterProvider, PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { W3CTraceContextPropagator } from '@opentelemetry/core';

// shim out a missing init or missing headers
const originalFetch = window.fetch;
window.fetch = (input, init = {}) => {
  if (!init.headers) init.headers = {};
  return originalFetch(input, init);
};

const resource = resourceFromAttributes({
  'service.name': 'cloud-petting-zoo-frontend',
});
diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.DEBUG);
const tracerProvider = new WebTracerProvider({
  resource,
  spanProcessors: [
    new SimpleSpanProcessor(new ConsoleSpanExporter()),
  ],
});
// Azure best practice: Configure appropriate endpoint based on environment
tracerProvider.register({ propagator: new W3CTraceContextPropagator()});

// Azure best practice: Configure auto-instrumentation with proper settings
registerInstrumentations({
  tracerProvider: tracerProvider,
  instrumentations: [
    new DocumentLoadInstrumentation(),
    new FetchInstrumentation({
      propagateTraceHeaderCorsUrls: [/.*/], // inject traceparent on *all* requests
      clearTimingResources: true,
    }),
    new XMLHttpRequestInstrumentation({
      propagateTraceHeaderCorsUrls: [/.*/],
    }),
  ],
});

// Export the tracer for manual spans
export const tracer = tracerProvider.getTracer('cloud-petting-zoo-frontend');

// Azure best practice: Add shutdown handler for clean teardown
const shutdown = () => {
  tracerProvider.shutdown()
    .then(() => console.log('Tracing terminated'))
    .catch((error) => console.log('Error terminating tracing', error))
    .finally(() => {
      // Clean up the fetch shim
      window.fetch = originalFetch;
    });
};

// Clean up on page unload
window.addEventListener('beforeunload', shutdown);
export { tracerProvider};