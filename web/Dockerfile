FROM rust:1.61 AS build
RUN rustup target add wasm32-unknown-unknown
RUN cargo install trunk

COPY . .
RUN cargo test 
RUN trunk build --release 

FROM nginx AS web
COPY --from=build ./dist/. /usr/share/nginx/html/.