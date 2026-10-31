# Microsoft's browser binaries and the npm runner must use matching versions.
FROM mcr.microsoft.com/playwright:v1.64.1-noble
WORKDIR /app
ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
RUN npm init -y && npm install --save-exact @playwright/test@1.64.1
COPY --chown=pwuser:pwuser smoke.spec.cjs ./
RUN chown pwuser:pwuser /app
USER pwuser
CMD ["npx", "playwright", "test", "smoke.spec.cjs", "--reporter=line", "--workers=1"]
