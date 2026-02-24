import * as React from "react";
import {
    Html,
    Head,
    Body,
    Container,
    Heading,
    Text,
    Button,
    Hr,
    Preview,
} from "@react-email/components";

export interface WalletInvitationProps {
    walletName: string;
    inviterName: string;
    token: string;
}

// App URL defaults to localhost for development; overridden by APP_URL env var in production.
const APP_URL = process.env.APP_URL ?? "http://localhost:5173";

// Email template for wallet invitation — renders an HTML email with an Accept button.
export function WalletInvitation({ walletName, inviterName, token }: WalletInvitationProps) {
    const acceptUrl = `${APP_URL}/wallets/invitations/${token}/accept`;

    return (
        <Html lang="en">
            <Head />
            <Preview>
                {inviterName} invited you to join "{walletName}" on Kakeibo
            </Preview>
            <Body style={styles.body}>
                <Container style={styles.container}>
                    <Heading style={styles.heading}>You've been invited to Kakeibo</Heading>

                    <Text style={styles.text}>
                        <strong>{inviterName}</strong> has invited you to join the shared wallet{" "}
                        <strong>"{walletName}"</strong> on Kakeibo — a mindful money management
                        platform for personal budgeting and collaborative expenses.
                    </Text>

                    <Button href={acceptUrl} style={styles.button}>
                        Accept invitation
                    </Button>

                    <Text style={styles.smallText}>
                        This invitation expires in 7 days. If you did not expect this email, you can
                        safely ignore it.
                    </Text>

                    <Hr style={styles.hr} />

                    <Text style={styles.footer}>
                        Alternatively, copy and paste this link into your browser:
                        <br />
                        <span style={styles.link}>{acceptUrl}</span>
                    </Text>
                </Container>
            </Body>
        </Html>
    );
}

const styles = {
    body: {
        backgroundColor: "#f9fafb",
        fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    },
    container: {
        margin: "40px auto",
        padding: "32px",
        maxWidth: "520px",
        backgroundColor: "#ffffff",
        borderRadius: "8px",
        border: "1px solid #e5e7eb",
    },
    heading: {
        fontSize: "22px",
        fontWeight: "700",
        color: "#111827",
        marginBottom: "16px",
    },
    text: {
        fontSize: "15px",
        color: "#374151",
        lineHeight: "1.6",
    },
    button: {
        display: "block",
        margin: "24px auto",
        padding: "12px 28px",
        backgroundColor: "#111827",
        color: "#ffffff",
        borderRadius: "6px",
        fontSize: "15px",
        fontWeight: "600",
        textDecoration: "none",
    },
    smallText: {
        fontSize: "13px",
        color: "#6b7280",
        lineHeight: "1.5",
    },
    hr: {
        borderColor: "#e5e7eb",
        margin: "24px 0",
    },
    footer: {
        fontSize: "12px",
        color: "#9ca3af",
        lineHeight: "1.5",
    },
    link: {
        color: "#4b5563",
        wordBreak: "break-all" as const,
    },
} as const;

export default WalletInvitation;
