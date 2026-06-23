export type LandingResult = {
  hero: { headline: string; subheadline: string };
  features: Array<{ title: string; description: string; icon: string }>;
  cta: { text: string; buttonLabel: string };
  design: {
    primaryColor: string;
    accentColor: string;
    backgroundColor: string;
    textColor: string;
    style: string;
  };
};

function isLight(hex: string): boolean {
  const clean = hex.replace("#", "").padEnd(6, "0");
  const r = parseInt(clean.slice(0, 2), 16);
  const g = parseInt(clean.slice(2, 4), 16);
  const b = parseInt(clean.slice(4, 6), 16);
  return (0.299 * r + 0.587 * g + 0.114 * b) / 255 > 0.5;
}

function contrast(bg: string): string {
  return isLight(bg) ? "#111111" : "#ffffff";
}

export default function LandingPreview({ result }: { result: LandingResult }) {
  const { hero, features, cta, design } = result;

  const heroText = contrast(design.primaryColor);
  const ctaText = contrast(design.accentColor);
  const ctaButtonBg = isLight(design.accentColor) ? design.primaryColor : "#ffffff";
  const ctaButtonText = contrast(ctaButtonBg);

  const isTechnical = design.style === "technical";
  const cardRadius =
    design.style === "playful"
      ? "1.25rem"
      : design.style === "minimal"
        ? "0.25rem"
        : "0.625rem";

  return (
    <div
      style={{
        fontFamily: isTechnical
          ? "var(--font-geist-mono), ui-monospace, monospace"
          : "var(--font-geist-sans), system-ui, sans-serif",
      }}
    >
      {/* Hero */}
      <section
        style={{
          backgroundColor: design.primaryColor,
          color: heroText,
          padding: "5.5rem 1.5rem 6.5rem",
        }}
      >
        <div
          style={{ maxWidth: "56rem", margin: "0 auto", textAlign: "center" }}
        >
          <h2
            style={{
              fontSize: "clamp(1.875rem, 5vw, 3.75rem)",
              fontWeight: 900,
              letterSpacing: "-0.025em",
              lineHeight: 1.1,
              marginBottom: "1.5rem",
            }}
          >
            {hero.headline}
          </h2>
          <p
            style={{
              fontSize: "clamp(1rem, 2vw, 1.25rem)",
              lineHeight: 1.65,
              opacity: 0.85,
              maxWidth: "40rem",
              margin: "0 auto",
            }}
          >
            {hero.subheadline}
          </p>
        </div>
      </section>

      {/* Features */}
      <section
        style={{
          backgroundColor: design.backgroundColor,
          padding: "5rem 1.5rem",
        }}
      >
        <div style={{ maxWidth: "72rem", margin: "0 auto" }}>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fit, minmax(260px, 1fr))",
              gap: "1.5rem",
            }}
          >
            {features.map((f, i) => (
              <div
                key={i}
                style={{
                  backgroundColor: "#ffffff",
                  borderRadius: cardRadius,
                  padding: "2rem",
                  border: `1px solid ${design.textColor}18`,
                  boxShadow: "0 1px 3px rgba(0,0,0,.05)",
                }}
              >
                <div style={{ fontSize: "2.25rem", marginBottom: "0.875rem" }}>
                  {f.icon}
                </div>
                <h3
                  style={{
                    fontWeight: 700,
                    fontSize: "1.0625rem",
                    color: design.textColor,
                    marginBottom: "0.5rem",
                  }}
                >
                  {f.title}
                </h3>
                <p
                  style={{
                    fontSize: "0.9375rem",
                    lineHeight: 1.65,
                    color: design.textColor,
                    opacity: 0.7,
                  }}
                >
                  {f.description}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section
        style={{
          backgroundColor: design.accentColor,
          color: ctaText,
          padding: "5rem 1.5rem",
          textAlign: "center",
        }}
      >
        <div style={{ maxWidth: "36rem", margin: "0 auto" }}>
          <p
            style={{
              fontSize: "clamp(1.0625rem, 2.5vw, 1.3125rem)",
              lineHeight: 1.7,
              fontWeight: 500,
              marginBottom: "2.5rem",
            }}
          >
            {cta.text}
          </p>
          <button
            style={{
              backgroundColor: ctaButtonBg,
              color: ctaButtonText,
              padding: "0.875rem 2.5rem",
              fontSize: "0.9375rem",
              fontWeight: 700,
              border: "none",
              borderRadius: "4px",
              cursor: "pointer",
              letterSpacing: "0.025em",
            }}
          >
            {cta.buttonLabel}
          </button>
        </div>
      </section>
    </div>
  );
}
