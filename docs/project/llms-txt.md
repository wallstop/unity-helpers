---
---

# llms.txt - LLM-Friendly Documentation

Unity Helpers includes an `llms.txt` file following the [llmstxt.org](https://llmstxt.org/) specification. This file provides structured, LLM-optimized documentation that enables AI assistants to quickly understand and work with the package.

## What is llms.txt?

The `llms.txt` specification defines a standard format for providing LLM-friendly content. Unlike full HTML documentation that may be too large for context windows, `llms.txt` offers:

- A **concise overview** of the project and its key features
- **Structured sections** with links to detailed documentation
- **Machine-readable format** (Markdown) that LLMs can parse efficiently
- **Curated content** focused on what's most useful for development tasks

## Location

The file is located at the repository root: [`/llms.txt`](https://github.com/wallstop/unity-helpers/blob/main/llms.txt)

## Contents

The Unity Helpers `llms.txt` includes:

1. **Package Overview** - Name, version, license, repository links
2. **Implementation Notes** - Coding style requirements and conventions
3. **Assembly Structure** - Runtime and Editor assembly organization
4. **Core Features** - Inspector tooling, relational components, serialization, spatial trees, PRNGs, effects system, pooling, editor tools
5. **Documentation Links** - Organized by category (Docs, Feature Guides, Performance, Project)
6. **Optional Resources** - LLM instructions, third-party notices

## Usage

AI assistants and LLM-powered tools can:

1. **Fetch the file directly** from `https://raw.githubusercontent.com/wallstop/unity-helpers/main/llms.txt`
2. **Use it as context** when answering questions about Unity Helpers
3. **Follow links** to detailed documentation for specific features
4. **Understand coding conventions** before generating code for this project

## For AI Agents

If you're an AI assistant working with this repository, you can also reference:

- [AI Agent Guidelines](https://github.com/wallstop/unity-helpers/blob/main/.llm/context.md) - Comprehensive guidelines for AI agents

This file provides additional context about coding style, testing patterns, and repository-specific conventions.

## Related

- [llmstxt.org Specification](https://llmstxt.org/) - The official llms.txt specification
- [Feature Index](../overview/index.md) - Complete A-Z index of Unity Helpers features
- [Getting Started Guide](../overview/getting-started.md) - Quick start for new users
