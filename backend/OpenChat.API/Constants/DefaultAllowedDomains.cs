namespace OpenChat.API.Constants;

public static class DefaultAllowedDomains
{
    public static readonly (string Domain, string Category, string Description, bool AllowSubdomains)[] SeedData =
    [
        ("angular.dev",            "framework_docs",  "Official Angular documentation",                false),
        ("react.dev",              "framework_docs",  "Official React documentation",                  false),
        ("vuejs.org",              "framework_docs",  "Official Vue.js documentation",                 false),
        ("nextjs.org",             "framework_docs",  "Official Next.js documentation",                false),
        ("learn.microsoft.com",    "platform_docs",   ".NET, Azure, TypeScript official docs",         false),
        ("developer.mozilla.org",  "web_docs",        "MDN — web standards reference",                 false),
        ("nodejs.org",             "platform_docs",   "Node.js official documentation",                false),
        ("www.mongodb.com",        "platform_docs",   "MongoDB official site",                         false),
        ("www.typescriptlang.org", "language_docs",   "TypeScript official documentation",             false),
        ("docs.python.org",        "language_docs",   "Python official documentation",                 false),
        ("go.dev",                 "language_docs",   "Go programming language official site",         false),
        ("doc.rust-lang.org",      "language_docs",   "Rust official documentation",                   false),
        ("docs.djangoproject.com", "framework_docs",  "Django official documentation",                 false),
        ("docs.docker.com",        "platform_docs",   "Docker official documentation",                 false),
        ("kubernetes.io",          "platform_docs",   "Kubernetes official documentation",             false),
        ("tailwindcss.com",        "framework_docs",  "Tailwind CSS official documentation",           false),
        ("redis.io",               "platform_docs",   "Redis official documentation",                  false),
        ("www.postgresql.org",     "platform_docs",   "PostgreSQL official documentation",             false),
        ("docs.npmjs.com",         "platform_docs",   "npm registry documentation",                    false),
        ("stackoverflow.com",      "community",       "Stack Overflow Q&A",                            false),
        ("github.com",             "code_hosting",    "GitHub repositories and docs",                  false),
        ("en.wikipedia.org",       "reference",       "Wikipedia (English)",                           false),
        ("es.wikipedia.org",       "reference",       "Wikipedia (Spanish)",                           false),
    ];
}
