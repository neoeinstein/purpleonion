RULESET = self-test
RULEIGNORE = rules.ignore
SEVERITY = medium+
CONFIDENCE = normal+

ifeq ($(CONFIG),DEBUG)
 BUILD_DIR =  $(top_srcdir)/build/Debug
endif
ifeq ($(CONFIG),RELEASE)
 BUILD_DIR =  $(top_srcdir)/build/Release
endif

gendarme: all
	gendarme --set $(RULESET) --ignore $(RULEIGNORE) --severity $(SEVERITY) \
		--confidence $(CONFIDENCE) $(BUILD_DIR)/Por*.{exe,dll}

gendarme-all: all
	gendarme --set $(RULESET) --ignore $(RULEIGNORE) --severity all \
		--confidence all $(BUILD_DIR)/Por*.{exe,dll}

