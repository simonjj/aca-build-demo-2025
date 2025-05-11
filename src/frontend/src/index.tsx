import './telemetry'; // Import telemetry before anything else
import React from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import reportWebVitals from './reportWebVitals';
import { diag, DiagConsoleLogger, DiagLogLevel } from '@opentelemetry/api';
diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.DEBUG);

const container = document.getElementById('root');
if (!container) throw new Error('Failed to find the root element');
const root = createRoot(container);

root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();

export {}; 