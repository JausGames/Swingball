FROM ubuntu:18.04 AS GameServer

WORKDIR /Swingball

COPY Builds/Server/ ./

# workaround
# wait until the sidecar is ready
CMD chmod +x ./Swingball_server.x86_64 && sleep 1 && ./Swingball_server.x86_64


# syntax=docker/dockerfile:1

FROM golang:1.19-alpine  AS MatchMakingAPI

WORKDIR /usr/src/app

COPY FleetAPI/go.mod FleetAPI/go.sum FleetAPI/*.go ./ 
COPY FleetAPI/.kube/config /root/.kube/

RUN go mod download && go mod verify

RUN go get agones.dev/agones/pkg/util/runtime@v1.30.0 \
&& go get github.com/spf13/viper@v1.7.0 \
&& go build -v -o /usr/local/bin/app ./...

EXPOSE 8080

CMD [ "app" ]