import { Hono } from 'hono';

const app = new Hono();

app.get('/health', (c) => c.text('OK'));

app.post('/render', async (c) => {
  // Placeholder - will be implemented in Task #4
  return c.json({ html: '<p>Email template placeholder</p>' });
});

const port = process.env.PORT || 3050;

console.log(`Email renderer starting on port ${port}`);

export default {
  port,
  fetch: app.fetch,
};
