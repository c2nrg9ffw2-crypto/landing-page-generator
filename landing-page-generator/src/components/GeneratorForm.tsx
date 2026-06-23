"use client";

import { useState } from "react";
import LandingPreview, { type LandingResult } from "./LandingPreview";

export default function GeneratorForm() {
  const [description, setDescription] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<LandingResult | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = description.trim();
    if (!trimmed) return;

    setIsLoading(true);
    setError(null);
    setResult(null);

    try {
      const res = await fetch("/api/generate", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ description: trimmed }),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error ?? "Generation failed");
      setResult(data as LandingResult);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <>
      {/* Generator panel */}
      <div style={{ backgroundColor: "#F6F5F3", padding: "3.5rem 1.5rem 3rem" }}>
        <div style={{ maxWidth: "42rem", margin: "0 auto" }}>
          {/* Signature header */}
          <div style={{ marginBottom: "2rem" }}>
            <span
              style={{
                display: "inline-block",
                fontSize: "0.8125rem",
                fontWeight: 800,
                letterSpacing: "0.2em",
                textTransform: "uppercase",
                color: "#0F1117",
                borderBottom: "2px solid #2B3BEF",
                paddingBottom: "0.25rem",
              }}
            >
              Landing Page Generator
            </span>
            <p
              style={{
                marginTop: "0.625rem",
                color: "#6B7280",
                fontSize: "0.9375rem",
                lineHeight: 1.55,
              }}
            >
              Describe your startup and get an instant landing page preview.
            </p>
          </div>

          {/* Form card */}
          <div
            style={{
              backgroundColor: "#ffffff",
              borderRadius: "8px",
              padding: "1.75rem",
              boxShadow:
                "0 1px 3px rgba(0,0,0,.08), 0 8px 24px rgba(0,0,0,.06)",
            }}
          >
            <form onSubmit={handleSubmit}>
              <label
                htmlFor="description"
                style={{
                  display: "block",
                  fontWeight: 600,
                  fontSize: "0.875rem",
                  color: "#0F1117",
                  marginBottom: "0.625rem",
                }}
              >
                Startup description
              </label>
              <textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="e.g. A CLI tool for developers that auto-generates beautiful API docs from TypeScript types. Zero configuration, searchable output."
                rows={6}
                disabled={isLoading}
                style={{
                  display: "block",
                  width: "100%",
                  border: "1px solid #E5E7EB",
                  borderRadius: "6px",
                  padding: "0.75rem 0.875rem",
                  fontSize: "0.9375rem",
                  lineHeight: 1.6,
                  resize: "vertical",
                  outline: "none",
                  fontFamily: "inherit",
                  color: "#0F1117",
                  boxSizing: "border-box",
                  backgroundColor: isLoading ? "#F9FAFB" : "#ffffff",
                  transition: "border-color 0.15s ease",
                }}
                onFocus={(e) => {
                  e.target.style.borderColor = "#2B3BEF";
                }}
                onBlur={(e) => {
                  e.target.style.borderColor = "#E5E7EB";
                }}
              />
              <div
                style={{
                  marginTop: "1rem",
                  display: "flex",
                  justifyContent: "flex-end",
                }}
              >
                <button
                  type="submit"
                  disabled={isLoading || !description.trim()}
                  style={{
                    backgroundColor:
                      isLoading || !description.trim() ? "#D1D5DB" : "#2B3BEF",
                    color:
                      isLoading || !description.trim() ? "#9CA3AF" : "#ffffff",
                    padding: "0.6875rem 1.75rem",
                    borderRadius: "4px",
                    border: "none",
                    fontWeight: 700,
                    fontSize: "0.875rem",
                    letterSpacing: "0.02em",
                    cursor:
                      isLoading || !description.trim()
                        ? "not-allowed"
                        : "pointer",
                    transition: "background-color 0.15s ease",
                    fontFamily: "inherit",
                  }}
                >
                  {isLoading ? "Generating…" : "Generate"}
                </button>
              </div>
            </form>
          </div>

          {/* Error state */}
          {error && (
            <div
              style={{
                marginTop: "1rem",
                padding: "0.75rem 1rem",
                backgroundColor: "#FEF2F2",
                border: "1px solid #FECACA",
                borderRadius: "6px",
                color: "#DC2626",
                fontSize: "0.875rem",
                lineHeight: 1.5,
              }}
            >
              <strong>Error:</strong> {error}
            </div>
          )}
        </div>
      </div>

      {/* Loading skeleton */}
      {isLoading && (
        <div>
          <div
            className="animate-pulse"
            style={{ height: "18rem", backgroundColor: "#D1D5DB" }}
          />
          <div
            style={{
              backgroundColor: "#F3F4F6",
              padding: "4rem 1.5rem",
            }}
          >
            <div
              style={{
                maxWidth: "72rem",
                margin: "0 auto",
                display: "grid",
                gridTemplateColumns: "repeat(auto-fit, minmax(260px, 1fr))",
                gap: "1.5rem",
              }}
            >
              {[0, 1, 2].map((i) => (
                <div
                  key={i}
                  className="animate-pulse"
                  style={{
                    height: "10rem",
                    backgroundColor: "#D1D5DB",
                    borderRadius: "8px",
                  }}
                />
              ))}
            </div>
          </div>
          <div
            className="animate-pulse"
            style={{ height: "14rem", backgroundColor: "#D1D5DB" }}
          />
        </div>
      )}

      {/* Generated landing page */}
      {result && !isLoading && <LandingPreview result={result} />}
    </>
  );
}
