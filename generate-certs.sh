#!/bin/bash
set -e

KEYS_DIR="./keys"
mkdir -p "$KEYS_DIR"

echo "Creating directory $KEYS_DIR..."
cd "$KEYS_DIR"

echo "Generating Root CA private key..."
openssl genrsa -out rootCA.key 4096

echo "Generating Root CA certificate..."
openssl req -x509 -new -nodes \
    -key rootCA.key \
    -sha256 -days 3650 \
    -out rootCA.pem \
    -subj "/C=US/ST=State/L=City/O=MyOrg/OU=Dev/CN=MyRootCA"

echo "Generating server private key..."
openssl genrsa -out private_key.pem 2048

echo "Generating CSR (Certificate Signing Request)..."
openssl req -new \
    -key private_key.pem \
    -out server.csr \
    -subj "/C=US/ST=State/L=City/O=MyOrg/OU=Dev/CN=localhost"

echo "Signing server certificate with Root CA..."
openssl x509 -req \
    -in server.csr \
    -CA rootCA.pem \
    -CAkey rootCA.key \
    -CAcreateserial \
    -out certificate.pem \
    -days 365 \
    -sha256

echo "Creating PFX (.p12) file..."
openssl pkcs12 -export \
    -out certificate.p12 \
    -inkey private_key.pem \
    -in certificate.pem

echo "Certificates generated in $KEYS_DIR"
