#!/bin/bash
echo "Building Rust library"
cd ../Solvers-Rust
cargo build --release
mkdir -p ./build
cp target/release/semgus_solvers_rust.dll ./build
