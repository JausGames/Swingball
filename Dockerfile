FROM ubuntu:18.04 AS GameServer

WORKDIR /Swingball

COPY Builds/Server/ ./

RUN apt install libvulkan1

# workaround
# wait until the sidecar is ready
CMD chmod +x ./Swingball_server.x86_64 && sleep 1 && ./Swingball_server.x86_64


# syntax=docker/dockerfile:1

FROM golang:1.19-alpine  AS MatchMakingAPI

WORKDIR /usr/src/app

COPY MatchMakingAPI/go.mod MatchMakingAPI/go.sum MatchMakingAPI/*.go ./ 
COPY MatchMakingAPI/.kube/config /root/.kube/

RUN go mod download && go mod verify

RUN go get agones.dev/agones/pkg/util/runtime@v1.30.0 \
&& go get github.com/spf13/viper@v1.7.0 \
&& go build -v -o /usr/local/bin/app ./...

EXPOSE 8080

CMD [ "app" ]