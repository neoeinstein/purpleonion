BINDIR = bin
SRCDIR = src
SOURCES = $(wildcard $(SRCDIR)/Xpdm.PurpleOnion/*.cs)
EXE = PurpleOnion.exe
RULESET = self-test
RULEIGNORE = rules.ignore
MONO_OPTIONS = $(SRCDIR)/Options.cs
REFERENCES = Mono.Security
UNIXHOST = YesPlease

ifneq ($(UNIXHOST),)
	REFERENCES += Mono.Posix
endif

$(BINDIR)/$(EXE): $(MONO_OPTIONS) $(SOURCES) $(BINDIR)
	gmcs -t:exe -out:$@ $(addprefix -r:,$(REFERENCES)) $(MONO_OPTIONS) $(SOURCES)

$(BINDIR):
	mkdir -p $(BINDIR)

$(MONO_OPTIONS):
	cp `pkg-config --variable=Sources mono-options` $(SRCDIR)

gendarme:
	gendarme --set $(RULESET) --ignore $(RULEIGNORE) $(BINDIR)/$(EXE)

clean:
	rm -f $(BINDIR)/*

distclean: clean
	rm -f $(MONO_OPTIONS)
