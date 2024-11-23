# Define variables
PYTHON := python3
SCRIPTS := pitchProcessor.py trillStateProcessor.py
PIDS := .pids

# Default target
.PHONY: all
all: run

# Run the scripts
.PHONY: run
run:
	@echo "Starting Python scripts..."
	@mkdir -p $(PIDS)
	@for script in $(SCRIPTS); do \
		$(PYTHON) $$script & echo $$! > $(PIDS)/$$script.pid; \
	done
	@echo "All scripts are running."

# Stop the scripts
.PHONY: clean
clean:
	@echo "Stopping Python scripts..."
	@if [ -d $(PIDS) ]; then \
		for pidfile in $(PIDS)/*.pid; do \
			kill $$(cat $$pidfile) 2>/dev/null || true; \
			rm -f $$pidfile; \
		done; \
		rmdir $(PIDS); \
	fi
	@echo "All scripts have been stopped."
