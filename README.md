# Kubernetes: dapr and distributed tracing with Azure Monitor

In this repository I want to show how distributed tracing works with [dapr](https://dapr.io) and Azure Monitor.
To demo a microservice architecture, the sample exists of four services. 

- FrontEnd Service
  The FrontEnd service accepts http requests and delegates them to one of the backend services. In order to have a simple and quick UI the FrontEnd service uses Swagger.
- Backend services
  To demo a distributed microservice architecture the FrontEnd service just calls an instance of either ServiceA, ServiceB or ServiceC. All services just returns a string containing the name of the service instance.

  // Todo: Architecture.

## Tracing in dapr

[dapr](https://dapr.io) uses [OpenTelemetry](https://opentelemetry.io) (previously known as OpenCensus) for distributed traces 
and metrics collection. You can define __exporters__ to export telemetry data 
to an endpoint that can handle the OpenTelemetry format. Dapr adds a HTTP/gRPC middleware to the Dapr sidecar. The middleware intercepts all Dapr and application traffic and automatically injects correlation IDs to trace distributed transactions.

![dapr tracing](/images/dapr-tracing-acrhitecture.png)

To setup tracing you need to create a tracing configuration for dapr:

```YAML
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: tracing
spec:
  tracing:
    enabled: true
    expandParams: true
    includeBody: true
```
To activate tracing for your deployment you need to add an additional dapr annotation:

```YAML
apiVersion: apps/v1
kind: Deployment
metadata:
  ...
spec:
  ...
  template:
    metadata:
      ...
      annotations:
        dapr.io/enabled: "true"
        dapr.io/id: "frontend"
        dapr.io/port: "5000"
        dapr.io/config: "tracing"
```

## Send Open Telemetry data to Application Insights

In order to push telemetry to an instance of __Application Insights__ an agent that understands the telemetry 
format must be used to transform and push the data to __Appplication Insights__. There is a component available named 
[LocalForwarder](https://docs.microsoft.com/de-de/azure/azure-monitor/app/opencensus-local-forwarder) that collects OpenCensus 
telemetry and routes it to Application Insights. LocalForwarder is an [open source project on GitHub](https://github.com/microsoft/ApplicationInsights-LocalForwarder).

__NOTE:__
At this time there is no official guidance on packaging and running LocalForwarder as aDocker container.
To use LocalForwarder in Kubernetes, you will need to package the LocalForwarder as a Docker container and register a *ClusterIP* service. I have already created a Docker image that is available on Docker Hub __m009/ai-local-forwarder__.
If you want to create your own image, you can clone the [LocalForwarder repository](https://github.com/microsoft/ApplicationInsights-LocalForwarder). There is a Docker file available under [examples/opencensus/local-forwarder](https://github.com/microsoft/ApplicationInsights-LocalForwarder/blob/master/examples/opencensus/local-forwarder). 

To export telemetry to the LocalForwarder you need to create an export component: 

```YAML
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: native
spec:
  type: exporters.native
  metadata:
  - name: enabled
    value: "true"
  - name: agentEndpoint
    value: "<Local forwarder address, for example: 50.140.60.170:6789>"
```


## Setup the demo
### Application Insights
1. Add ApplicationInsights extension to Azure CLI:
   ```Shell
   az extension add -n application-insights
   ```
2. Create an ApplicationInsight resource:
   ```Shell
   az monitor app-insights component create --app aidapr --location eastus2 --resource-group <your RG> --kind web --application-type web
   ```
   Copy the value of the *instrumentationKey*, we will need it later.

### LocalForwarder
1. Open the [deployment file](/deploy/localforwarder-deployment.yaml) and replace the value of __<AI instrumentationKey>__ with 
   the instrumentationKey of the previous step.
2. Deploy the LocalForwarder to your cluster.
   ```Shell
   kubectl apply -f ./deploy/localforwarder-deployment.yaml
   ```

### dapr tracing
1. Deploy the dapr tracing configuration:
   ```
   kubectl apply -f ./deploy/dapr-tracing.yaml
   ```
2. Deploy the exporter:
   ```Shell
   kubectl apply -f ./deploy/dapr-tracing-exporter.yaml
   ```
   Note that the dns name *ai-local-forwarder.default.svc.cluster.local* for the ClusterIP service is used.

### Deploy the services

1. Deploy the microservices:
   ``` Shell
   kubectl apply -f ./deploy/servicea-deployment.yaml
   kubectl apply -f ./deploy/serviceb-deployment.yaml
   kubectl apply -f ./deploy/servicec-deployment.yaml
   kubectl apply -f ./deploy/frontend-deployment.yaml
   ```
   
2. Get the public Ip of the frontend service and check if it is working:
   ``` Shell
   kubectl get service frontendservice
   ```
   Copy the Ip and use __curl__ to check the service
   ``` Shell
   curl -X GET http://<frontendIp>/FrontEnd/serviceA
   ```
   You should see the following output:
   ``` Shell
   Hello World, ServiceA
   
   ```

### Generate data and inspect ApplicatinInsights' Application Map

No let us create some telemetry:
```Shell
curl -X GET http://<frontendIp>/frontend/servicea
curl -X GET http://<frontendIp>/frontend/serviceb
curl -X GET http://<frontendIp>/frontend/servicec
```

We have to wait a few minutes until ApplicationInsights updates the Application Map.
Browse to your ApplicationInsights resource an have a look at the Application Map.

![Application map](/images/ai-applicationmap.png)