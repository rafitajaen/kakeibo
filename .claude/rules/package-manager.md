# Package Manager

**Bun is the only supported package manager.** Never use `npm`, `npx`, `pnpm`, or `yarn`.

| Command | Use |
|---------|-----|
| `bun install` | Install dependencies |
| `bun install --frozen-lockfile` | Install with frozen lockfile (CI) |
| `bun run <script>` | Run a script defined in `package.json` |
| `bunx <tool>` | Runs the tool with its own runtime (Node-compatible default). Use for most tools: `commitlint`, `oxlint`, `lefthook`. |
| `bunx --bun <tool>` | Forces the tool to run under the Bun runtime. Required for tools that must use Bun's APIs: `bunx --bun shadcn-vue@latest add button`. |
