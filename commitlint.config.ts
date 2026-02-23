import type { UserConfig } from '@commitlint/types'

const config: UserConfig = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-enum': [
      2, 'always',
      ['app', 'api', 'email', 'docs', 'infra', 'deps', 'release', 'skills', 'roadmap'],
    ],
    'scope-empty': [0],
    'subject-empty': [2, 'never'],
    'subject-max-length': [2, 'always', 100],
    'type-empty': [2, 'never'],
  },
}

export default config
