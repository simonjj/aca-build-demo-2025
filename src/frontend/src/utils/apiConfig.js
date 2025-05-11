import { context, SpanStatusCode } from '@opentelemetry/api';
// Azure best practice: Import only the tracer from telemetry.js, not the exporter
import { tracer } from '../telemetry';
// Azure best practice: Add missing imports for context propagation
import { trace } from '@opentelemetry/api';

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
  // Azure best practice: Create a parent span with semantic attributes
  const parentSpan = tracer.startSpan('getPetState', {
    attributes: { 
      petType, 
      petId: petId || '', 
      'azure.service': 'container-app',
      'azure.component': 'frontend'
    }
  });
  
  // Azure best practice: Use context propagation for proper span hierarchy
  return context.with(trace.setSpan(context.active(), parentSpan), async () => {
    try {
      // Azure best practice: Create child spans for significant operations
      const urlSpan = tracer.startSpan('getApiUrl', { attributes: { petType }});
      const apiUrl = getApiUrl(petType);
      urlSpan.end();
      
      const endpoint = `/pet/state${petId ? `?id=${petId}` : ''}`;
      const url = `${apiUrl}${endpoint}`;
      
      // Azure best practice: Record detailed request information
      parentSpan.setAttribute('http.url', url);
      parentSpan.setAttribute('http.method', 'GET');
      
      // Azure best practice: Create child span for the fetch operation
      const fetchSpan = tracer.startSpan('fetch', {
        attributes: {
          'http.url': url,
          'http.method': 'GET'
        }
      });
      
      // Track timing manually for detailed performance analysis
      const fetchStart = Date.now();
      
      const response = await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      });
      
      // Azure best practice: Record fetch performance and results
      fetchSpan.setAttribute('http.status_code', response.status);
      fetchSpan.setAttribute('http.duration_ms', Date.now() - fetchStart);
      fetchSpan.setAttribute('success', response.ok);
      fetchSpan.end();

      // Azure best practice: Create child span for JSON parsing (can be slow for large payloads)
      const parseSpan = tracer.startSpan('parseJson');
      const data = await response.json();
      parseSpan.end();
      
      if (!response.ok) {
        // Azure best practice: Create error event for failures
        parentSpan.addEvent('error', {
          'error.type': 'HttpError',
          'error.code': response.status,
          'error.message': response.statusText
        });
        
        throw new Error(`Failed to fetch pet state: ${response.statusText}`);
      }
      
      // Azure best practice: Record business-level attributes
      parentSpan.setAttribute('pet.mood', data.mood || 'unknown');
      parentSpan.setAttribute('pet.energy', data.energy || 0);
      
      return data;
    } catch (error) {
      // Azure best practice: Record exception with details
      parentSpan.recordException(error);
      parentSpan.setStatus({
        code: SpanStatusCode.ERROR,
        message: error.message
      });
      
      throw error;
    } finally {
      // Azure best practice: End the span which triggers export through configured processor
      parentSpan.end();
    }
  });
};

/**
 * Interact with a pet via the backend API with Azure best practice tracing
 * @param {string} petType - The type of pet
 * @param {string} action - The interaction action (pet, feed, poke, sing, message)
 * @param {string} message - Optional message for the pet
 * @param {string} petId - Optional pet ID for multi-instance pets
 * @returns {Promise<Object>} The updated pet state
 */
export const interactWithPet = async (petType, action, message = null, petId = null) => {
  // Azure best practice: Create span with business context attributes
  const span = tracer.startSpan('interactWithPet', {
    attributes: { 
      petType, 
      action,
      'azure.service': 'container-app',
      'azure.component': 'frontend',
      'business.transaction': 'pet_interaction'
    }
  });

  return context.with(trace.setSpan(context.active(), span), async () => {
    try {
      const apiUrl = getApiUrl(petType);
      const endpoint = '/pet/interact';
      const url = `${apiUrl}${endpoint}`;
      
      const payload = {
        action,
        ...(message && { message }),
        ...(petId && { id: petId })
      };
      
      // Azure best practice: Record detailed request payload attributes
      span.setAttribute('http.url', url);
      span.setAttribute('http.method', 'POST');
      span.setAttribute('request.action', action);
      span.setAttribute('request.size', JSON.stringify(payload).length);
      
      // Azure best practice: Record operation start for SLO monitoring
      span.addEvent('operation.start');
      const operationStart = Date.now();

      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify(payload)
      });
      
      // Azure best practice: Record operation duration for performance monitoring
      const duration = Date.now() - operationStart;
      span.addEvent('operation.end', { durationMs: duration });
      span.setAttribute('http.duration_ms', duration);
      
      // Azure best practice: Record SLI metrics as span attributes
      if (duration > 1000) {
        span.setAttribute('slo.exceeded', true);
        span.setAttribute('slo.threshold_ms', 1000);
      }
      
      span.setAttribute('http.status_code', response.status);

      if (!response.ok) {
        // Azure best practice: Structured error recording
        span.addEvent('error', {
          'error.type': 'HttpError',
          'error.code': response.status,
          'error.message': response.statusText
        });
        
        throw new Error(`Failed to interact with ${petType}: ${response.statusText}`);
      }
      
      const data = await response.json();
      
      // Azure best practice: Record business outcome
      span.setAttribute('outcome.success', true);
      span.setAttribute('pet.mood_changed', data.mood !== payload.mood);
      
      return data;
    } catch (error) {
      // Azure best practice: Record exception details
      span.recordException(error);
      span.setStatus({
        code: SpanStatusCode.ERROR,
        message: error.message
      });
      
      // Azure best practice: Record business outcome for errors
      span.setAttribute('outcome.success', false);
      span.setAttribute('error.detail', error.message);
      
      throw error;
    } finally {
      // Azure best practice: Always end spans to ensure they're exported
      span.end();
    }
  });
};