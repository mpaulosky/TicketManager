import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { picksFrom } from "./plan.mts";

const ready = [{ number: 3 }, { number: 5 }, { number: 8 }];

describe("picksFrom", () => {
	it("keeps the ready issues the plan names, in plan order", () => {
		assert.deepEqual(picksFrom(["8", "3"], ready, () => {}), [{ number: 8 }, { number: 3 }]);
	});

	it("skips an id that isn't ready", () => {
		const warnings: string[] = [];
		assert.deepEqual(picksFrom(["4", "5"], ready, (line) => warnings.push(line)), [{ number: 5 }]);
		assert.match(warnings[0]!, /Skipping 4/);
	});

	it("takes a repeated id once, however it's written", () => {
		const warnings: string[] = [];
		assert.deepEqual(picksFrom(["5", "#5", " 5", "3", "5"], ready, (line) => warnings.push(line)), [{ number: 5 }, { number: 3 }]);
		assert.equal(warnings.length, 3);
	});
});
