# swoosh-web

Vue 3 + TypeScript + Vite frontend for Swoosh. See the [root README](../../README.md) for full project documentation.

## Commands

```bash
npm install      # Install dependencies
npm run dev      # Dev server (http://localhost:5173)
npm run build    # Type-check + production build
npm run lint     # ESLint
```

## Environment

Create a `.env.local` file to point at your API:

```env
VITE_API_URL=http://localhost:5250/api
```

The default (`http://localhost:5250/api`) is used when no env file is present.
