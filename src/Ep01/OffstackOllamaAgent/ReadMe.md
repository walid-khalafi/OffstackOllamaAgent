# OffstackOllamaAgent

> **Stop paying for cloud AI. It’s time to own it!**

Welcome to the official repository for the **OFFStack** local AI architecture series. This project demonstrates how to orchestrate a high-performance LLM entirely offline, locally, and for free inside a lightweight .NET console environment.

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blueviolet.svg)](https://dotnet.microsoft.com/download)
[![Ollama](https://img.shields.io/badge/Ollama-Local%20AI-orange.svg)](https://ollama.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 📺 Watch the Tutorial
This codebase accompanies our step-by-step video guide on YouTube. 
👉 **[Watch the full video here](https://youtu.be/KlLEjmNQ9I8)**

---

## ⚡ Features
* **100% Local & Private:** No cloud dependencies, no subscription fees, and complete data sovereignty.
* **Microsoft.Extensions.AI Stack:** Built using Microsoft's modern, decoupled abstraction layer (`IChatClient`).
* **Real-time Streaming:** Token-by-token terminal streaming using `GetStreamingResponseAsync` for zero latency feel.
* **Architect Identity:** Tailored system instructions making the local model act as an Elite Enterprise Architect.

---

## 🛠️ Tech Stack & Dependencies
* **Runtime:** .NET 9.0+ (Console Application)
* **LLM Engine:** [Ollama](https://ollama.com/) running `llama3.1:8b`
* **Core Packages:**
  * `Microsoft.Extensions.AI.Ollama` (v9.7.0-preview.1.25356.2)
  * `Microsoft.Agents.AI`

---

### 1. Prerequisites
First, ensure you have **Ollama** installed and the model pulled locally:
```bash
# Pull the Llama 3.1 8B model
ollama pull llama3.1:8b
