// The reviewer's verdict. The reviewer ends its run with
// <verdict>{"approved": true|false, "summary": "..."}</verdict>; the host
// parses it and publishes only an approved branch. A missing or malformed
// verdict counts as a rejection, so a review that went wrong never lets
// unreviewed work through.

import { z } from "zod";

const verdictSchema = z.object({
	approved: z.boolean(),
	summary: z.string().trim().min(1),
});

export type Verdict = z.infer<typeof verdictSchema>;

// The JSON inside the last <verdict> tag in the agent's output, without a
// surrounding code fence, or undefined when there is no complete tag.
function lastVerdictContent(output: string): string | undefined {
	const end = output.lastIndexOf("</verdict>");
	if (end === -1) return undefined;
	const start = output.lastIndexOf("<verdict>", end);
	if (start === -1) return undefined;
	return output
		.slice(start + "<verdict>".length, end)
		.trim()
		.replace(/^```(?:json)?\s*\n?/, "")
		.replace(/\n?```$/, "")
		.trim();
}

export function parseVerdict(output: string): Verdict {
	const content = lastVerdictContent(output);
	if (content === undefined) {
		return { approved: false, summary: "The reviewer ended without a <verdict>, so the change counts as rejected." };
	}
	let json: unknown;
	try {
		json = JSON.parse(content);
	} catch {
		return { approved: false, summary: "The reviewer's <verdict> wasn't valid JSON, so the change counts as rejected." };
	}
	const parsed = verdictSchema.safeParse(json);
	if (!parsed.success) {
		return {
			approved: false,
			summary: "The reviewer's <verdict> didn't have a boolean `approved` and a `summary`, so the change counts as rejected.",
		};
	}
	return parsed.data;
}
