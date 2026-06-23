import Anthropic from "@anthropic-ai/sdk";

const SYSTEM_PROMPT = `You are a landing page copywriter and brand designer. Given a startup description, return ONLY raw JSON — no markdown fences, no commentary, nothing else before or after.

The JSON must match this exact schema:
{
  "hero": {
    "headline": "5–10 word punchy headline capturing the core promise",
    "subheadline": "1–2 sentences on value proposition and who it's for"
  },
  "features": [
    { "title": "Feature name", "description": "1–2 sentences on this capability", "icon": "one emoji" },
    { "title": "Feature name", "description": "1–2 sentences on this capability", "icon": "one emoji" },
    { "title": "Feature name", "description": "1–2 sentences on this capability", "icon": "one emoji" }
  ],
  "cta": {
    "text": "1–2 sentence paragraph that motivates action and creates urgency",
    "buttonLabel": "2–4 word action phrase"
  },
  "design": {
    "primaryColor": "#hex — dominant brand color used as the hero section background",
    "accentColor": "#hex — contrasting accent used as the CTA section background",
    "backgroundColor": "#hex — light near-white background for the features section",
    "textColor": "#hex — dark body text color for use on light backgrounds",
    "style": "one of: minimal | bold | playful | technical | elegant | warm"
  }
}

Choose a palette that genuinely fits the startup's world:
- Wellness / meditation → calming blues, sage greens, warm neutrals; style: warm or minimal
- Developer tools / CLI → precise grays, electric blues, deep navies; style: technical or minimal
- Gaming / entertainment → bold saturated high-contrast colors; style: bold or playful
- Fintech / enterprise → professional blues, greens, cool grays; style: minimal or elegant
- Creative / design tools → opinionated, distinctive; style: elegant or bold

Avoid generic defaults: no cream-and-terracotta unless it truly fits, no purple-gradient-on-dark.`;

export async function POST(request: Request) {
  try {
    const body = await request.json();
    const description = body?.description;

    if (
      !description ||
      typeof description !== "string" ||
      description.trim().length === 0
    ) {
      return Response.json({ error: "Description is required" }, { status: 400 });
    }

    const client = new Anthropic();
    const message = await client.messages.create({
      model: "claude-sonnet-4-6",
      max_tokens: 1024,
      system: SYSTEM_PROMPT,
      messages: [{ role: "user", content: description.trim() }],
    });

    const textBlock = message.content.find((b) => b.type === "text");
    if (!textBlock || textBlock.type !== "text") {
      return Response.json({ error: "No response from AI" }, { status: 500 });
    }

    let raw = textBlock.text.trim();
    if (raw.startsWith("```")) {
      raw = raw.replace(/^```(?:json)?\n?/, "").replace(/\n?```$/, "").trim();
    }

    const result = JSON.parse(raw);
    return Response.json(result);
  } catch (err) {
    const msg = err instanceof Error ? err.message : "Unexpected error";
    return Response.json({ error: msg }, { status: 500 });
  }
}
