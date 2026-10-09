// What an agent wrapped in <tag>…</tag> as its answer.

// The content of the last complete <tag>…</tag> in the agent's output, trimmed
// and without a surrounding code fence, or undefined when there is none.
export function lastTagContent(output: string, tag: string): string | undefined {
	const end = output.lastIndexOf(`</${tag}>`);
	if (end === -1) return undefined;
	const start = output.lastIndexOf(`<${tag}>`, end);
	if (start === -1) return undefined;
	return output
		.slice(start + `<${tag}>`.length, end)
		.trim()
		.replace(/^```(?:json)?\s*\n?/, "")
		.replace(/\n?```$/, "")
		.trim();
}
