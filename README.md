# 🦴 Cloud Petting Zoo: Chaos, Cuties & Containers

**Tagline:** 20,000 people. 5 adorable creatures. Total API madness.

**Demo Goal:** Show off how Azure Container Apps handles burst traffic, microservices, autoscaling, and stateful coordination in a funny, live, and memorable way.

## 🐾 The Experience (for the audience)

### Scan a QR Code → Choose a Creature
Each user chooses which animal to pet, feed, annoy, or cheer up.

### Creatures could be:
- 🐢 ChillTurtle
- 🐙 EmoOcto
- 🐉 ChaosDragon
- 🦖 BabyDino
- 🐇 BouncyBun

### Interaction Options per Creature:
- **Pet** → Increases happiness
- **Feed** → Increases energy
- **Poke** → Increases chaos
- **Sing To** → Calms it
- **Send Message** → Lets the crowd write what it hears

### Big Screen Visualization:
- Real-time emotional state of each pet (via emojis, color, animation)
- Pop-up bubbles with funny AI-generated thoughts from pets ("why is everyone feeding me broccoli?")
- Pet evolution (e.g., Dragon gets wings if fed too much)

## ⚙️ Architecture: What It Demonstrates

| Real-World Problem | Pet Zoo Feature | Azure Container Apps Feature |
|-------------------|-----------------|------------------------------|
| 🧠 Handling burst traffic | 20,000+ users spamming API calls | Autoscaling of backend pods on demand |
| 🪢 Event-driven coordination | Pets evolve or react in stages based on thresholds | Dapr pub/sub or Event Grid for coordination |
| 🔁 Live state management | Pet mood, energy, actions update in real time | Stateful microservices via containerized APIs |
| 🧵 Decoupled services | Each pet runs its own service, evolves independently | Microservices model deployed as isolated containers |
| 🪵 Observability | Show how chaos affects response time or pet health | Log streaming + Application Insights for real metrics |
| 🎭 Resiliency testing | Simulate people DDoS-ing ChaosDragon | Show restart logic / scaling across regions |
| 🧪 A/B or canary testing | Some users get a "grumpy" version of Bunny | Revision-based traffic splitting |
| 📱 Frontend delivery | Thousands load petting UI instantly | Host via Static Web Apps or Azure Front Door over Container Apps backend |

## 🧱 High-Level Architecture

```
                         +----------------+
     Phones (users) ---> |   Frontend UI  |  ← static web app
                         +--------+-------+
                                  |
         +------------------------+------------------------+
         |                        |                        |
     +---v---+              +-----v-----+            +-----v-----+
     | 🐙 OctoAPI | <--->   | 🐇 BunAPI  |   <--->    | 🐢 TurtleAPI |
     +---+---+              +-----+-----+            +-----+-----+
         |                        |                        |
    +----v----+             +-----v-----+            +-----v-----+
    | State DB |             | Redis/Dapr|            | CosmosDB |
    +---------+             +-----------+            +-----------+
```

- 👁️ **Observability:** App Insights, Logs
- 📊 **Realtime:** SignalR for WebSocket push
- 🔄 **Pub/Sub:** Dapr or Event Grid between pets
- 🧠 **AI Thoughts:** Azure OpenAI API per pet

## 💥 Demo Flow

| Time | Demo Moment | Azure Concept |
|------|-------------|---------------|
| 0:00 | Everyone starts tapping "Pet the Bunny" → server spike | Show autoscaling in action |
| 0:45 | Show real-time graph: "You're generating 5,000 RPS" | Azure Metrics dashboard |
| 1:15 | Turtle overwhelmed → stops responding for 10s | Simulate latency or pod crash |
| 1:45 | Bun evolves into "MegaBun" — confetti + chaos | Triggered by crowd milestone via Dapr |
| 2:00 | Show logs: chaos = dropped events = auto recovery | Real-time logging + recovery demo |
| 2:30 | Ask crowd to "feed Octopus" → food limit hits | Rate limiting logic & fallback pod shown |
| 3:00 | Bunny users get rerouted to new revision | Traffic split + A/B test results |


## Features of this Demo

- Private Endpoint + AFD (GA, showing this as part of the architecture diagram, [sample](https://github.com/microsoft/azure-container-apps/tree/main/templates/bicep/privateEndpointFrontDoor) )
- Planned maintenance (GA, slide only, potentially a drive-by-mention during demo)
- Aspire Dashboard + OTEL (GA, in-app and in-demo, potentially with 3rd party)
- Path-based routing (Preview, in-app and in-demo [sample](https://github.com/Tratcher/HttpRouteConfigBicep) )
- Workload Profile metrics (not sure yet, we'll have to see what they look like)
- Durable Task Scheduler (in-app, in-demo [sample](https://github.com/Azure-Samples/Durable-Task-Scheduler/tree/main/samples/portable-sdks/dotnet/FunctionChaining) )