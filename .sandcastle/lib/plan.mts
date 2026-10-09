// The planner's plan, and the issues the host takes from it.

import { z } from "zod";
import { lastTagContent } from "./tags.mts";

// The planner emits its plan as JSON inside <plan> tags; parsePlan extracts
// and validates it against this schema. There's no branch field: the host
// names branches, so a re-plan can't move an issue's work to a new branch.
export const planSchema = z.object({
	issues: z.array(z.object({ id: z.string(), title: z.string() })),
});

// The plan in the planner's output. Throws when the tag is missing, the JSON is
// malformed or it doesn't match the schema, which stops the run.
export function parsePlan(output: string): z.infer<typeof planSchema> {
	const content = lastTagContent(output, "plan");
	if (content === undefined) throw new Error("The planner ended without a <plan>.");
	let json: unknown;
	try {
		json = JSON.parse(content);
	} catch {
		throw new Error("The planner's <plan> wasn't valid JSON.");
	}
	const parsed = planSchema.safeParse(json);
	if (!parsed.success) throw new Error(`The planner's <plan> didn't match its schema: ${parsed.error.message}`);
	return parsed.data;
}

// The ready issues the plan picks, in plan order. An id that isn't one of the
// ready issues is skipped, so a hallucinated or stale id can't start work on
// an issue that wasn't offered, and a repeated id counts once, so two
// pipelines never build the same issue on the same branch.
export function picksFrom<T extends { number: number }>(
	planIds: readonly string[],
	ready: readonly T[],
	warn: (line: string) => void = console.warn,
): T[] {
	const picks: T[] = [];
	const seen = new Set<number>();
	for (const id of planIds) {
		const issue = ready.find((open) => String(open.number) === id.trim().replace(/^#/, ""));
		if (!issue) {
			warn(`  Skipping ${id}: it isn't one of the ready issues.`);
		} else if (seen.has(issue.number)) {
			warn(`  Skipping ${id}: the plan lists it more than once.`);
		} else {
			seen.add(issue.number);
			picks.push(issue);
		}
	}
	return picks;
}
