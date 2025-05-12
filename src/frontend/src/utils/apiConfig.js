// Azure best practice: Import only the tracer from telemetry.js, not the exporter
import { tracer } from '../telemetry';
import { context, trace, propagation, SpanStatusCode } from '@opentelemetry/api';
// Azure best practice: Add missing imports for context propagation
/**
 * API configuration for Azure Container Apps backend services
 * Following Azure best practices for frontend-backend integration
 */

// Map pet types to their corresponding environment variables
const petApiEnvMap = {
  'turtle': 'REACT_APP_API_TURTLE',
  'octo': 'REACT_APP_API_OCTO', 
  'dragon': 'REACT_APP_API_DRAGON',
  'dino': 'REACT_APP_API_DINO',
  'bunny': 'REACT_APP_API_BUNNY'
};
  
/**
 * Gets the API URL for a specific pet type using environment variables
 * @param {string} petType - The type of pet (turtle, octo, dragon, dino, bunny)
 * @returns {string} The complete API URL for the pet service
 */
export const getApiUrl = (petType) => {
  // Azure best practice: Create a span for even small operations to track performance
  const span = tracer.startSpan('getApiUrl', {
    attributes: { petType }
  });
  
  try {
    const normalizedPetType = petType?.toLowerCase();
    const envKey = petApiEnvMap[normalizedPetType];
    let result;
    
    if (!envKey || !process.env[envKey]) {
      // Azure best practice: Record application warnings in spans
      span.setAttribute('warning', 'api_url_not_configured');
      span.setAttribute('fallback_used', true);
      
      console.warn(`No API URL configured for pet type: ${petType}, falling back to default API`);
      result = process.env.REACT_APP_API_URL || 'http://localhost:3001';
    } else {
      result = process.env[envKey];
    }
    
    // Azure best practice: Record important return values
    span.setAttribute('api.url', result);
    return result;
  } finally {
    // Azure best practice: Always end spans
    span.end();
    // The completed span will automatically be processed by the configured
    // span processor and sent to Azure Monitor through the traceExporter
  }
};
  
/**
 * Get the pet state from the appropriate backend service with Azure best practice tracing
 * @param {string} petType - The type of pet
 * @param {string} petId - Optional pet ID for multi-instance pets
 * @returns {Promise<Object>} The pet state object
 */
export const getPetState = async (petType, petId = null) => {
  // 1️⃣ start the root span with semantic attributes
  const parentSpan = tracer.startSpan('getPetState', {
    attributes: {
      'pet.type':          petType,
      'pet.id':            petId || '',
      'azure.service':     'container-app',
      'azure.component':   'frontend',
      'http.method':       'GET',
    },
  });

  // 2️⃣ bind it into context & inject only W3C headers
  const ctx = trace.setSpan(context.active(), parentSpan);
  const headers = {};
  propagation.inject(ctx, headers);

  try {
    // 3️⃣ build URL
    const apiUrl   = getApiUrl(petType);
    const endpoint = `/pet/state${petId ? `?id=${petId}` : ''}`;
    const url      = `${apiUrl}${endpoint}`;

    // record URL on the span
    parentSpan.setAttribute('http.url', url);

    // 4️⃣ do your fetch with the traceparent header
    const response = await fetch(url, {
      headers: {
        ...headers,
        'Accept':        'application/json',
        'Content-Type':  'application/json',
      }
    });

    const data = await response.json();

    if (!response.ok) {
      parentSpan.setStatus({
        code:    SpanStatusCode.ERROR,
        message: response.statusText
      });
      throw new Error(`Failed to fetch pet state: ${response.statusText}`);
    }

    return data;
  } catch (err) {
    parentSpan.recordException(err);
    parentSpan.setStatus({
      code:    SpanStatusCode.ERROR,
      message: err.message
    });
    throw err;
  } finally {
    // 5️⃣ close out the root span
    parentSpan.end();
  }
};

/**
 * Interact with a pet via the backend API with Azure best practice tracing
 * @param {string} petType - The type of pet
 * @param {string} action - The interaction action (pet, feed, poke, sing, message)
 * @param {string} message - Optional message for the pet
 * @param {string} petId - Optional pet ID for multi-instance pets
 * @returns {Promise<Object>} The updated pet state
 */
export const interactWithPet = async (
  petType,
  action,
  message = null,
  petId   = null
) => {
  // 1️⃣ start your root span with biz context
  const span = tracer.startSpan('interactWithPet', {
    attributes: {
      'pet.type':            petType,
      'interaction.action':  action,
      'azure.service':       'container-app',
      'azure.component':     'frontend',
      'business.transaction':'pet_interaction',
      'http.method':         'POST',
    },
  });

  // 2️⃣ bind it into a new context & inject traceparent header
  const ctx     = trace.setSpan(context.active(), span);
  const headers = {};
  propagation.inject(ctx, headers);

  try {
    // build URL + payload
    const apiUrl   = getApiUrl(petType);
    const url      = `${apiUrl}/pet/interact`;
    const payload  = {
      action,
      ...(message && { message }),
      ...(petId   && { id: petId   }),
    };

    // record HTTP details
    span.setAttribute('http.url',    url);
    span.setAttribute('request.action', action);
    span.setAttribute('request.size',  JSON.stringify(payload).length);

    // mark operation start
    span.addEvent('operation.start');
    const startMs = Date.now();

    // 3️⃣ do the fetch under the same context
    const response = await context.with(ctx, () =>
      fetch(url, {
        method:  'POST',
        headers: {
          ...headers,
          'Content-Type': 'application/json',
          'Accept':        'application/json'
        },
        body: JSON.stringify(payload),
      })
    );

    // record timing & end event
    const duration = Date.now() - startMs;
    span.addEvent('operation.end', { durationMs: duration });
    span.setAttribute('http.duration_ms', duration);
    if (duration > 1000) {
      span.setAttribute('slo.exceeded',      true);
      span.setAttribute('slo.threshold_ms', 1000);
    }

    span.setAttribute('http.status_code', response.status);

    if (!response.ok) {
      span.addEvent('error', {
        'error.type':    'HttpError',
        'error.code':     response.status,
        'error.message':  response.statusText
      });
      span.setStatus({
        code:    SpanStatusCode.ERROR,
        message: response.statusText
      });
      throw new Error(`Failed to interact with ${petType}: ${response.statusText}`);
    }

    // parse result + record business outcome
    const data = await response.json();
    span.setAttribute('outcome.success',    true);
    span.setAttribute('pet.mood_changed',   data.mood !== payload.mood);

    return data;
  } catch (err) {
    // record exception & mark span errored
    span.recordException(err);
    span.setStatus({
      code:    SpanStatusCode.ERROR,
      message: err.message
    });
    span.setAttribute('outcome.success', false);
    span.setAttribute('error.detail',     err.message);

    throw err;
  } finally {
// Azure best practice: Always end spans to ensure they're exported
    span.end();
  }
};