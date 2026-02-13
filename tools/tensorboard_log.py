
# Script to log CSV metrics to TensorBoard
import argparse
import csv
import io
import os
import time



# Get a TensorBoard SummaryWriter from available libraries
def get_writer(logdir):
    try:
        from tensorboardX import SummaryWriter
        return SummaryWriter(logdir)
    except Exception:
        try:
            from torch.utils.tensorboard import SummaryWriter
            return SummaryWriter(logdir)
        except Exception as exc:
            raise RuntimeError(
                "No TensorBoard writer found. Install tensorboardX or torch."
            ) from exc



# Log health metrics from CSV rows to TensorBoard
def log_health_rows(writer, rows):
    for row in rows:
        step = int(row["step"])
        writer.add_scalar("health/queen", float(row["queen_health"]), step)
        writer.add_scalar("health/avg_worker", float(row["avg_worker_health"]), step)
        writer.add_scalar("world/nest_blocks", float(row["nest_blocks"]), step)
        writer.add_scalar("world/mulch_consumed", float(row["mulch_consumed"]), step)
        writer.add_scalar("world/alive_count", float(row["alive_count"]), step)
        writer.add_scalar("time/remaining", float(row["time_remaining"]), step)
        writer.add_scalar("generation/index", float(row["generation"]), step)



# Log generation metrics from CSV rows to TensorBoard
def log_generation_rows(writer, rows):
    for row in rows:
        step = int(row["generation"])
        writer.add_scalar("fitness/best", float(row["best_fitness"]), step)
        writer.add_scalar("fitness/avg", float(row["avg_fitness"]), step)
        writer.add_scalar("world/nest_blocks_per_gen", float(row["nest_blocks"]), step)



# Log all rows from a CSV file using the provided logging function
def log_csv_file(writer, csv_path, log_rows_fn):
    if not os.path.exists(csv_path):
        return

    with open(csv_path, newline="") as handle:
        reader = csv.DictReader(handle)
        log_rows_fn(writer, list(reader))



# Helper class to read new rows from a growing CSV file (like tail -f)
class CsvTailer:
    def __init__(self, csv_path, start_at_end):
        self.csv_path = csv_path
        self.position = 0
        self.buffer = ""
        self.header = None

        # If starting at end, skip to end of file
        if start_at_end and os.path.exists(csv_path):
            with open(csv_path, "r", encoding="utf-8") as handle:
                handle.seek(0, os.SEEK_END)
                self.position = handle.tell()

    # Read any new rows added to the CSV since last check
    def read_new_rows(self):
        if not os.path.exists(self.csv_path):
            return []

        with open(self.csv_path, "r", encoding="utf-8") as handle:
            handle.seek(self.position)
            data = handle.read()
            self.position = handle.tell()

        if not data:
            return []

        text = self.buffer + data
        lines = text.splitlines()
        if text and not text.endswith(("\n", "\r")):
            self.buffer = lines.pop() if lines else text
        else:
            self.buffer = ""

        if not lines:
            return []

        # Handle CSV header
        if self.header is None:
            self.header = lines[0]
            lines = lines[1:]
        else:
            if lines and lines[0].strip() == self.header.strip():
                lines = lines[1:]

        if not lines:
            return []

        csv_text = "\n".join([self.header] + lines)
        reader = csv.DictReader(io.StringIO(csv_text))
        return list(reader)



# Main function to parse arguments and run logging
def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--metrics-dir", required=True, help="Directory containing CSV metrics")
    parser.add_argument("--logdir", default="tb_logs", help="TensorBoard log output directory")
    parser.add_argument("--follow", action="store_true", help="Stream updates until interrupted")
    parser.add_argument("--poll", type=float, default=1.0, help="Polling interval in seconds")
    parser.add_argument("--tail", action="store_true", help="Start streaming from end of CSVs")
    args = parser.parse_args()

    metrics_dir = args.metrics_dir
    health_csv = os.path.join(metrics_dir, "health_metrics.csv")
    gen_csv = os.path.join(metrics_dir, "generation_metrics.csv")

    # If metrics files not found, try to read latest.txt for directory
    if not os.path.exists(health_csv) and not os.path.exists(gen_csv):
        latest_path = os.path.join(metrics_dir, "latest.txt")
        if os.path.exists(latest_path):
            with open(latest_path, "r", encoding="utf-8") as handle:
                metrics_dir = handle.read().strip()
            health_csv = os.path.join(metrics_dir, "health_metrics.csv")
            gen_csv = os.path.join(metrics_dir, "generation_metrics.csv")


    writer = get_writer(args.logdir)

    # If not following, just log all rows and exit
    if not args.follow:
        log_csv_file(writer, health_csv, log_health_rows)
        log_csv_file(writer, gen_csv, log_generation_rows)
        writer.flush()
        writer.close()
        return

    # If following, stream updates from CSVs
    health_tailer = CsvTailer(health_csv, args.tail)
    gen_tailer = CsvTailer(gen_csv, args.tail)

    try:
        while True:
            health_rows = health_tailer.read_new_rows()
            gen_rows = gen_tailer.read_new_rows()
            if health_rows:
                log_health_rows(writer, health_rows)
            if gen_rows:
                log_generation_rows(writer, gen_rows)
            if health_rows or gen_rows:
                writer.flush()
            time.sleep(max(0.1, args.poll))
    except KeyboardInterrupt:
        writer.flush()
        writer.close()



# Run main if this script is executed
if __name__ == "__main__":
    main()
