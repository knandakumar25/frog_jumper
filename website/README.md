# Axiom Force — Web Portal & Player Hub

The official web portal and companion dashboard for **Axiom Force**, built with **React**, **Vite**, and **Tailwind CSS**.

## Features

- **Cyberpunk Command Dashboard**: Neon-aesthetic interface matching the in-game UI.
- **Operative Dossiers**: In-depth lore, attributes, and signature ability breakdowns for all 6 operatives:
  - Cyber Shogun (Kaito Kurogane)
  - Surya Prime (Arjun Varma)
  - Oceanus (Kiana Vaea)
  - Mirage Blade (Zayd Al-Mansoor)
  - Apex Titan (King Tariq Nzinga)
  - Aura Phoenicia (Maya Khoury)
- **Live Leaderboards**: Integration with Django REST Framework backend for high scores and level completion stats.
- **Authentication**: User account management with Clerk and Django backend integration.

## Architecture & Integration

- **Frontend**: React 19, Vite, Tailwind CSS, Lucide icons.
- **Backend API**: Django REST Framework (Python 3.14) running at `http://localhost:8000/api/`.

## Development Setup

### Local Development
```bash
# Install dependencies
npm install

# Start Vite dev server (runs at http://localhost:5173/)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint
```
