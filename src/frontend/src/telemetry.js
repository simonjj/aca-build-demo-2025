// src/telemetry.js - Azure Container Apps Best Practice Implementation
import { resourceFromAttributes } from '@opentelemetry/resources';
import { diag, DiagConsoleLogger, DiagLogLevel } from '@opentelemetry/api';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor, SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter} from '@opentelemetry/exporter-trace-otlp-http';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-http';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { MeterProvider, PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';

// shim out a missing init or missing headers
const originalFetch = window.fetch;
window.fetch = (input, init = {}) => {
  if (!init.headers) init.headers = {};
  return originalFetch(input, init);
};

// function createResourceAttrs() {
//   return {
//     'service.name': 'cloud-petting-zoo-frontend',
//     'service.version': '1.0.0',
//     //'deployment.environment': process.env.NODE_ENV || 'development',
//   };
// }

// const resource = new Resource(createResourceAttrs());

// const envResource = await detectResources();

// // (b) build your own custom attrs
// const customResource = resourceFromAttributes({
//   'service.name': 'cloud-petting-zoo-frontend',
//   'service.version': '1.0.0',
//   //'deployment.environment': process.env.NODE_ENV || 'development',
// });

// (c) merge them together
const resource = resourceFromAttributes({
  'service.name': 'cloud-petting-zoo-frontend',
  /* etc */
});
// Azure best practice: Create resource without direct Resource import
// const createResource = () => {
//   // Create simple resource with key attributes
//   return {
//     'service.name': 'cloud-petting-zoo-frontend',
//     'service.version': '1.0.0',
//     'deployment.environment': process.env.NODE_ENV || 'development'
//   };
// };
console.log('[OTEL] telemetry.js loaded');
diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.DEBUG);
console.log('[OTEL] telemetry.js loaded');
// ─── Tracing Setup ─────────────────────────────────────────────────────────────
// Azure best practice: Create tracer with proper resource identificatio

// Azure best practice: Configure appropriate endpoint based on environment
const tracerProvider = new WebTracerProvider({resource: resource});
tracerProvider.addSpanProcessor(
  new SimpleSpanProcessor(new ConsoleSpanExporter())
);
tracerProvider.register();

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

// ─── Metrics Setup ─────────────────────────────────────────────────────────────
// Azure best practice: Configure metric exporter with proper endpoint
// const metricExporter = new OTLPMetricExporter();

// // Azure best practice: Use PeriodicExportingMetricReader with appropriate interval
// const meterProvider = new MeterProvider({
//      resource: resource,
//      metricReaders: [
//        new PeriodicExportingMetricReader({
//          exporter: metricExporter,
//          exportIntervalMillis: process.env.NODE_ENV === 'production' ? 30000 : 1000,
//        }),
//      ],
//    });

// // Export the meter for manual metrics
// export const meter = meterProvider.getMeter('cloud-petting-zoo-frontend');

// Azure best practice: Add shutdown handler for clean teardown
const shutdown = () => {
  tracerProvider.shutdown()
    .then(() => console.log('Tracing terminated'))
    .catch((error) => console.log('Error terminating tracing', error))
    .finally(() => {
      // meterProvider.shutdown()
      //   .then(() => console.log('Metrics terminated'))
      //   .catch((error) => console.log('Error terminating metrics', error));
    });
};

// Clean up on page unload
window.addEventListener('beforeunload', shutdown);

// ────────────────────────────────────────────────────────────────────────────────
export { tracerProvider};