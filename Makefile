BUILD = bin/
SOURCES = $(wildcard src/Xpdm.PurpleOnion/*.cs)
EXE = PurpleOnion.exe
RULESET = default

$(EXE): $(SOURCES)
	mkdir -p $(BUILD)
	gmcs -t:exe -out:$(BUILD)$@ -r:Mono.Security $(SOURCES)

gendarme:
	gendarme --config rules.xml --set $(RULESET) $(BUILD)$(EXE)

clean:
	rm -f $(BUILD)*
