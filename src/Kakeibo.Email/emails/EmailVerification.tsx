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

export interface EmailVerificationProps {
    token: string;
}

// App URL defaults to localhost for development; overridden by APP_URL env var in production.
const APP_URL = process.env.APP_URL ?? "http://localhost:5173";

// Email template for email address verification — renders an HTML email with a Verify button.
export function EmailVerification({ token }: EmailVerificationProps) {
    const verifyUrl = `${APP_URL}/verify-email?token=${token}`;

    return (
        <Html lang="en">
            <Head />
            <Preview>Verify your email address to activate your Kakeibo account</Preview>
            <Body style={styles.body}>
                <Container style={styles.container}>
                    <Heading style={styles.heading}>Verify your email address</Heading>

                    <Text style={styles.text}>
                        Thanks for signing up for Kakeibo! Please verify your email address to
                        activate your account and start tracking your finances mindfully.
                    </Text>

                    <Button href={verifyUrl} style={styles.button}>
                        Verify email address
                    </Button>

                    <Text style={styles.smallText}>
                        This link expires in 24 hours. If you did not create a Kakeibo account, you
                        can safely ignore this email.
                    </Text>

                    <Hr style={styles.hr} />

                    <Text style={styles.footer}>
                        Alternatively, copy and paste this link into your browser:
                        <br />
                        <span style={styles.link}>{verifyUrl}</span>
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

export default EmailVerification;
