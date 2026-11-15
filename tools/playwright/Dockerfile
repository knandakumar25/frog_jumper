# Microsoft's browser binaries and the npm runner must use matching versions.
FROM mcr.microsoft.com/playwright:v1.64.1-noble
WORKDIR /app
ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
RUN npm init -y && npm install --save-exact @playwright/test@1.64.1 @clerk/testing@2.2.55 @clerk/backend@3.27.0 axios@1.18.1
COPY --chown=pwuser:pwuser smoke.spec.cjs ./
COPY --chown=pwuser:pwuser browser-auth.spec.cjs ./
COPY --chown=pwuser:pwuser account-security.spec.cjs client-refresh.spec.cjs ./
COPY --chown=pwuser:pwuser playwright.config.cjs ./
RUN chown pwuser:pwuser /app
USER pwuser
CMD ["npx", "playwright", "test", "smoke.spec.cjs", "--reporter=line", "--workers=1"]
