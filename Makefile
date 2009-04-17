BUILD = bin/
SOURCES = $(wildcard src/Xpdm.PurpleOnion/*.cs)
EXE = PurpleOnion.exe
RULESET = self-test
RULEIGNORE = rules.ignore

$(EXE): $(SOURCES)
	mkdir -p $(BUILD)
	gmcs -t:exe -out:$(BUILD)$@ -r:Mono.Security $(SOURCES)

gendarme:
	gendarme --set $(RULESET) --ignore $(RULEIGNORE) $(BUILD)$(EXE)

clean:
	rm -f $(BUILD)*
