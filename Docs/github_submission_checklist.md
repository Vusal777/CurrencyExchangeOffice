# GitHub Submission Checklist

Use this checklist before submitting the repository link in Moodle.

## Required

- [ ] Repository is public.
- [ ] Repository contains `CurrencyExchangeOffice.sln`.
- [ ] Repository contains WCF service source code.
- [ ] Repository contains WPF client source code.
- [ ] Repository contains database scripts.
- [ ] Repository contains documentation.
- [ ] Repository contains `README.md`.
- [ ] `README.md` includes course name.
- [ ] `README.md` includes project title.
- [ ] `README.md` includes author name.
- [ ] `README.md` includes student ID.
- [ ] `README.md` includes short project description.
- [ ] `README.md` includes run instructions.
- [ ] Project builds successfully in Visual Studio.

## Recommended Commit History

Create multiple commits instead of one final commit. Example commit messages:

```text
Initial WCF service structure
Add NBP exchange rate integration
Add user accounts and balances
Add currency buy and sell operations
Add WPF client interface
Add database scripts
Improve UI and account workflow
Add README and documentation
```

## Do Not Upload

These files should be ignored by `.gitignore`:

- `.vs/`
- `bin/`
- `obj/`
- compiled `.exe` files
- compiled `.dll` files
- user-specific Visual Studio files

