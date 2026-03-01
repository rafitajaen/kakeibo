import { Hono } from "hono";
import { render } from "@react-email/render";
import { WalletInvitation, type WalletInvitationProps } from "./emails/WalletInvitation";
import { EmailVerification, type EmailVerificationProps } from "./emails/EmailVerification";

const app = new Hono();

app.get("/health", (c) => c.text("OK"));

// Renders an email template to HTML. Body: { template: string, props: Record<string, unknown> }
// Supported templates: EmailVerification, WalletInvitation
app.post("/render", async (c) => {
    const body = await c.req.json<{ template: string; props: Record<string, unknown> }>();

    if (body.template === "EmailVerification") {
        const props = body.props as unknown as EmailVerificationProps;
        const html = await render(EmailVerification(props));
        return c.json({ html });
    }

    if (body.template === "WalletInvitation") {
        const props = body.props as unknown as WalletInvitationProps;
        const html = await render(WalletInvitation(props));
        return c.json({ html });
    }

    return c.json({ error: `Unknown template: ${body.template}` }, 400);
});

const port = process.env.PORT || 3050;

console.log(`Email renderer starting on port ${port}`);

export default {
    port,
    fetch: app.fetch,
};
