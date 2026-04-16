# ADR-0018: Relational Data Access Applicability

**Status:** Accepted
**Date:** 2026-04-16

## Context

Requested doctrine includes an EF Core plus Dapper hybrid, but the current
repository is a local Windows tray app with file-based configuration and no
relational database. Adding database doctrine that the product does not use
would turn ADR into fiction.

## Decision

- The current repository does not add EF Core or Dapper because no relational
  persistence surface exists today.
- If a future repository change introduces relational persistence:
  - writes and schema evolution default to EF Core,
  - complex read models may use Dapper where EF-generated SQL is a poor fit.
- No relational persistence packages or configuration should be added before a
  concrete feature requires them.

## Alternatives Considered

| Option                                                   | Pros                                          | Cons                                                 |
| -------------------------------------------------------- | --------------------------------------------- | ---------------------------------------------------- |
| Add EF Core and Dapper doctrine immediately              | Matches a future-looking preference           | Misstates the actual product and adds dead weight    |
| Ignore the topic entirely                                | Keeps current scope clean                     | Leaves future relational work without guidance       |
| Record the applicability rule and defer package adoption | Honest about current scope and future default | Requires a later ADR update when a DB really appears |

## Consequences

- **Positive:** Repository doctrine stays truthful about the current product.
- **Positive:** A future relational subsystem still has a default strategy.
- **Negative:** The repository does not showcase that hybrid stack yet.
- **Risks:** Future contributors could still add a database ad hoc unless the
  ADR is cited in reviews and rules.
