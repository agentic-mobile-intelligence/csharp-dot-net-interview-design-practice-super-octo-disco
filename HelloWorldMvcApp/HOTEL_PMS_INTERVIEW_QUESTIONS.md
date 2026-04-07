# Hotel PMS ETL Pipeline: Deep Technical Interview Questions & Answers

## 🎯 Complete Interview Questions

### Architecture & Scale

**Q1: How would you handle 500k room transactions daily from multiple properties?**

Answer shows: scalability thinking, batch processing, parallel architecture
- Stream-based Excel reading (not loading all into memory)
- Process in 5-10k batches
- Run multiple properties in parallel
- EFCore.BulkExtensions for 50-60x performance
- Checkpointing after each batch

**Q2: What happens if an Excel file is corrupted mid-upload or contains 100k rows with validation errors?**

Answer shows: error resilience, operational awareness
- File integrity validation first
- Partial success strategy (load valid, skip invalid)
- All-or-nothing strategy (for financial data)
- Store failed rows for manual review
- Fallback readers (EPPlus → ExcelDataReader)

**Q3: Would you process synchronously or async? Why?**

Answer shows: blocking/scalability understanding
- Background jobs (Hangfire): ASYNC required
- HTTP endpoints: ASYNC + fire-and-forget
- Don't block users waiting for 10-minute import
- Use job status polling
- Async all the way down for I/O operations

---

### Data Quality

**Q4: Walk me through how you'd detect duplicate reservations across files from different properties.**

Answer shows: data integrity thinking
- Composite unique key (PropertyId + GuestId + CheckIn + CheckOut + RoomNumber)
- Detect duplicates within current batch (HashSet)
- Check against existing database
- Find "soft" duplicates (overlapping dates, same room)
- Database-level constraints for ultimate safety

**Q5: How do you handle missing or malformed dates in the Excel sheets?**

Answer shows: data validation strategy
- FluentValidation with custom date parsers
- Fallback date formats (MM/DD/YYYY, YYYY-MM-DD, etc.)
- Null handling (required vs. optional)
- Reject clearly invalid dates (1900, 2050)
- Log each failure with row context

**Q6: What's your strategy for handling PII (guest names, card data) in transit?**

Answer shows: security awareness
- Encrypt data at rest (Entity Framework value converters)
- TLS for data in transit
- Never log PII
- GDPR compliance (right to be forgotten)
- Separate secure storage for sensitive data
- Audit trail of who accessed what

---

### Performance

**Q7: Why use bulk insert over row-by-row EF operations?**

Answer shows: performance reasoning
- Row-by-row: 10k rows = 8-10 minutes
- Bulk insert: 10k rows = 3-5 seconds
- 50-60x faster with EFCore.BulkExtensions
- Why: reduces network round-trips and connection overhead
- Connection opens: 10k vs. 1
- Network overhead per row vs. per batch

**Q8: How would you optimize reading a massive Excel file without loading it entirely into memory?**

Answer shows: memory-conscious design
- ExcelDataReader: lightweight, streaming
- Forward-only reading (can't seek back)
- Process in streaming fashion
- Parse row-by-row, don't accumulate
- Flush to database every N rows
- For truly massive files: split into multiple smaller files

**Q9: Where would you add indexing in the database schema for fast lookups?**

Answer shows: database design thinking
- Composite index on (PropertyId, GuestId, CheckInDate, CheckOutDate)
- Index on (PropertyId, RoomNumber, CheckInDate) for availability checks
- Index on CreatedAt for time-range queries
- Index on Status for filtering (Confirmed, Cancelled)
- Monitor query execution plans
- Don't over-index (slows writes)

---

### Error Handling

**Q10: A batch fails halfway through. How do you recover without losing state or creating duplicates?**

Answer shows: transactional thinking, idempotency
- Checkpointing: record progress after each batch
- Idempotent operations: can replay without side effects
- Use transactions for atomic batch operations
- Log last successful checkpoint
- Resume from last checkpoint on retry
- Deduplicate: if row exists, skip (don't insert twice)

**Q11: An Excel file has 10k bad rows. Do you fail the whole batch or skip invalid rows? What's your trade-off?**

Answer shows: operational trade-offs

Partial Success (skip bad rows):
- Pro: Keep 90k valid rows instead of losing all
- Con: Partial data might cause reporting issues
- Use when: Daily sync, acceptable to have gaps
- Example: Guest list import (missing 100 guests is manageable)

All-or-Nothing (fail whole batch):
- Pro: Data integrity, no partial state
- Con: Lose all 100k rows because 10k are bad
- Use when: Financial reconciliation, can't have gaps
- Example: Revenue ledger (can't have partial month)

**Q12: How do you alert the hotel ops team when something breaks?**

Answer shows: operational awareness
- Serilog to centralized logging (Seq)
- SMS/email alerts for critical failures
- Hangfire dashboard for job status
- Integration with PagerDuty for escalation
- Slack notifications for warnings
- Dashboard showing import health (success rate, duration)

---

### Pipeline Design

**Q13: What's your validation layer look like — before load or after?**

Answer shows: architectural decision-making

Before Load (Validate-Then-Load):
- Pro: Don't insert invalid data
- Pro: Clearer error messages (row-level)
- Con: Validation is CPU-bound, blocks loading
- Code: Use FluentValidation on DTOs

After Load (Load-Then-Validate):
- Pro: Get data into temp table fast, then validate
- Pro: Can do complex SQL-based validation (overlaps, uniqueness)
- Con: Invalid data in DB (need rollback)
- Code: Insert to staging table, validate, merge to production

Hybrid (Validate-Then-Load with post-checks):
- Quick validation before load (format, nulls)
- Deep validation after load (uniqueness, overlaps)
- Most resilient approach

**Q14: How often should the pipeline run, and how do you prevent concurrent runs colliding?**

Answer shows: scheduling thinking
- Frequency: depend on business (daily, hourly, real-time?)
- Prevention: Use Hangfire's built-in locking
- Or: Explicit lock in database (reservation_import_lock)
- Or: Check if previous job still running, skip if true
- Monitor: Alert if job takes too long (indicates backlog)

**Q15: Would you stage raw data first, then transform? Why or why not?**

Answer shows: pipeline architecture

Stage First (Extract → Raw Table → Transform → Load):
- Pro: Easier to debug (inspect raw data)
- Pro: Can re-transform with new logic
- Con: Requires double storage
- Pro: Data lineage clear

Transform-Then-Load (Extract → Transform → Load):
- Pro: No staging table overhead
- Con: Can't inspect raw data if transform fails
- Con: If logic changes, re-import source file

Best: Stage-First for critical pipelines (audit trail matters)

---

### Monitoring

**Q14: How would you know if the pipeline succeeded but silently dropped 100 rows?**

Answer shows: data quality thinking
- Compare: Rows in → Rows out + Failed rows
- Alert if (In ≠ Out + Failed)
- Log each row's status (SUCCESS, FAILED, SKIPPED)
- Row-level audit table
- Dashboard showing import rate vs. database row count
- Reconciliation job: count Excel rows vs. database rows

**Q15: What metrics matter most — latency, throughput, error rate, or data completeness?**

Answer shows: operational priorities

| Metric | Importance | Why |
|--------|-----------|-----|
| Data Completeness | ⭐⭐⭐ Critical | Silent drops are worse than slow imports |
| Error Rate | ⭐⭐⭐ Critical | Need to know if rows failed |
| Latency | ⭐⭐ Important | But not at cost of data integrity |
| Throughput | ⭐⭐ Important | 500k rows in 5min vs. 10min |

---

### Real-World Curveballs

**Q16: Different hotel chains send data in different Excel formats. How do you handle schema drift?**

Answer shows: flexibility thinking
- Define a standard schema
- Map each chain's format to standard
- ConfigurationFactory for each chain's rules
- Schema versioning (v1, v2 of import format)
- Accept old format for N months, then require new
- Document transformation mapping

**Q17: What if a property manager manually edits the Excel file while it's being processed?**

Answer shows: concurrency thinking
- File locking (Windows: other process can't edit while reading)
- Or: Copy file before processing
- Or: Version control (timestamp in filename)
- Or: Expected: ETL runs at fixed time, don't edit then
- Checksum file before/after to detect changes
- Alert if file size changed during import

**Q18: How do you version your transformations if business logic changes mid-year?**

Answer shows: maintainability thinking
- Transformation version in code (TransformationV1, V2)
- Load date stamped on each row
- Can run V1 for old data, V2 for new
- Or: Backfill all data with V2 (if logic compatible)
- Document changes: "V2 now caps nightly rate at $10k"
- Keep old transformers for audit trail

---

## 📊 Decision Matrix: When to Use What

| Problem | Solution | Code Pattern |
|---------|----------|--------------|
| 500k rows daily | Batch + Parallel | ParallelOptions, BulkInsert |
| Corrupted Excel | File validation + fallback | IsValidExcelFile, ExcelDataReader |
| Duplicates | Composite key + HashSet | .GroupBy().Where(g => g.Count > 1) |
| Slow inserts | EFCore.BulkExtensions | await _context.BulkInsertAsync() |
| Memory bloat | Stream reading | IAsyncEnumerable<T> |
| Silent drops | Row-level logging | _logger.LogInformation per row |
| Concurrent runs | Hangfire locking | RecurringJobManager |
| Schema drift | Factory pattern | IPropertySectorFactory |

---

## 🎓 Interview Tips

1. **Explain your thinking out loud** - "I'd use bulk insert because row-by-row has too much overhead"
2. **Know the trade-offs** - "We could fail the whole batch, but partial success means we don't lose data"
3. **Ask clarifying questions** - "How critical is data integrity vs. speed?"
4. **Reference production experience** - "I've seen this fail when..."
5. **Discuss monitoring** - "How would we know if this breaks at 3 AM?"

---

**These questions probe whether you think operationally, handle edge cases, and understand the difference between "it works" and "it works at scale."** 🏨
