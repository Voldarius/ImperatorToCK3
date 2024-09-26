﻿using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Government;

internal sealed class GovernmentMapping {
	public string CK3GovernmentId { get; private set; } = "";
	public SortedSet<string> ImperatorGovernmentIds { get; } = [];
	public SortedSet<string> ImperatorCultureIds { get; } = [];
	public HashSet<string> RequiredCK3Dlcs { get; } = [];

	public GovernmentMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ck3", reader => CK3GovernmentId = reader.GetString());
		parser.RegisterKeyword("ir", reader => ImperatorGovernmentIds.Add(reader.GetString()));
		parser.RegisterKeyword("irCulture", reader => ImperatorCultureIds.Add(reader.GetString()));
		parser.RegisterKeyword("has_ck3_dlc", reader => RequiredCK3Dlcs.Add(reader.GetString()));
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(mappingReader);
	}

	public string? Match(string irGovernmentId, string? irCultureId, IReadOnlyCollection<string> enabledCK3Dlcs) {
		if (!ImperatorGovernmentIds.Contains(irGovernmentId)) {
			return null;
		}

		if (ImperatorCultureIds.Count != 0) {
			if (irCultureId is null) {
				return null;
			}
			if (!ImperatorCultureIds.Contains(irCultureId)) {
				return null;
			}
		}
		
		if (RequiredCK3Dlcs.Count != 0) {
			if (enabledCK3Dlcs.Count == 0) {
				return null;
			}
			if (!RequiredCK3Dlcs.IsSubsetOf(enabledCK3Dlcs)) {
				return null;
			}
		}

		return CK3GovernmentId;
	}
}