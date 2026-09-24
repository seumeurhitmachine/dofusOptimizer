---
project: {{PROJECT_NAME}}
date: {{DATE_YYYY-MM-DD}}
stack_observee: {{STACK_OBSERVEE}}
reference: {{ARCHITECTURE_REFERENCE}}
type: audit-migration
---

# Rapport d'audit migration vers {{ARCHITECTURE_REFERENCE}}

## 1. Synthese de conformite

`{{PROJECT_NAME}}` {{SYNTHESE_POSITIONNEMENT}}.

Conforme ou partiellement conforme :

- {{POINT_CONFORME_1}}
- {{POINT_CONFORME_2}}
- {{POINT_CONFORME_3}}

Ecarts structurants :

- {{ECART_STRUCTURANT_1}}
- {{ECART_STRUCTURANT_2}}
- {{ECART_STRUCTURANT_3}}

Conclusion : {{CONCLUSION_AUDIT}}.

## 2. Cartographie technique observee

Structure observee :

```text
type: {{TYPE_REPO}}
package manager: {{PACKAGE_MANAGER}}
workspace: {{WORKSPACE}}
app: {{APP_STRUCTURE}}
api: {{API_STRUCTURE}}
prisma: {{PRISMA_STRUCTURE}}
infra: {{INFRA_STRUCTURE}}
docker: {{DOCKER_ETAT}}
github actions: {{GITHUB_ACTIONS_ETAT}}
pm2/caddy: {{PM2_CADDY_ETAT}}
node attendu: {{NODE_ATTENDU}}
```

Fichiers structurants observes :

| Zone | Fichiers |
|---|---|
| Scripts | {{FICHIERS_SCRIPTS}} |
| Frontend | {{FICHIERS_FRONTEND}} |
| API | {{FICHIERS_API}} |
| Prisma | {{FICHIERS_PRISMA}} |
| Client API | {{FICHIERS_CLIENT_API}} |
| Tests | {{FICHIERS_TESTS}} |
| Exploitation | {{FICHIERS_EXPLOITATION}} |

## 3. Frontend

Observations :

- {{OBS_FRONTEND_1}}
- {{OBS_FRONTEND_2}}
- {{OBS_FRONTEND_3}}

Classement des ecarts frontend :

| Ecart | Classement |
|---|---|
| {{ECART_FRONTEND_1}} | {{CLASSEMENT_FRONTEND_1}} |
| {{ECART_FRONTEND_2}} | {{CLASSEMENT_FRONTEND_2}} |

## 4. Backend

Observations generales :

- {{OBS_BACKEND_1}}
- {{OBS_BACKEND_2}}
- {{OBS_BACKEND_3}}

Modules importants :

| Module | Routes | Schemas | Service/repository | Tests | Ecarts |
|---|---|---|---|---|---|
| {{MODULE_1}} | {{ROUTES_1}} | {{SCHEMAS_1}} | {{SERVICE_REPOSITORY_1}} | {{TESTS_1}} | {{ECARTS_1}} |
| {{MODULE_2}} | {{ROUTES_2}} | {{SCHEMAS_2}} | {{SERVICE_REPOSITORY_2}} | {{TESTS_2}} | {{ECARTS_2}} |

## 5. Authentification

Conforme :

- {{AUTH_CONFORME_1}}
- {{AUTH_CONFORME_2}}

Ecarts au standard :

- {{AUTH_ECART_1}}
- {{AUTH_ECART_2}}
- {{AUTH_ECART_3}}

{{AUTH_COMMENTAIRE_RISQUE}}

## 6. Prisma et base

Observations :

- {{OBS_PRISMA_1}}
- {{OBS_PRISMA_2}}
- {{OBS_PRISMA_3}}

Risques schema identifies :

| Sujet | Observation | Risque |
|---|---|---|
| {{SUJET_SCHEMA_1}} | {{OBS_SCHEMA_1}} | {{RISQUE_SCHEMA_1}} |
| {{SUJET_SCHEMA_2}} | {{OBS_SCHEMA_2}} | {{RISQUE_SCHEMA_2}} |

## 7. Docker, portabilite, exploitation

Observations :

- {{OBS_INFRA_1}}
- {{OBS_INFRA_2}}
- {{OBS_INFRA_3}}

Distinction :

| Besoin | Etat observe | Commentaire |
|---|---|---|
| Dev quotidien | {{ETAT_DEV_QUOTIDIEN}} | {{COMMENTAIRE_DEV_QUOTIDIEN}} |
| Reproduction prod | {{ETAT_REPROD_PROD}} | {{COMMENTAIRE_REPROD_PROD}} |
| Production | {{ETAT_PRODUCTION}} | {{COMMENTAIRE_PRODUCTION}} |

## 8. CI/CD

Observations :

- {{OBS_CICD_1}}
- {{OBS_CICD_2}}
- {{OBS_CICD_3}}

Ordre CD observe :

1. {{CD_OBSERVE_1}}
2. {{CD_OBSERVE_2}}
3. {{CD_OBSERVE_3}}

Ordre cible :

1. {{CD_CIBLE_1}}
2. {{CD_CIBLE_2}}
3. {{CD_CIBLE_3}}

Ecarts :

- {{ECART_CICD_1}}
- {{ECART_CICD_2}}

## 9. Tableau priorise des ecarts

| Domaine | Ecart | Standard cible | Impact | Priorite | Migration |
|---|---|---|---|---|---|
| {{DOMAINE_1}} | {{ECART_1}} | {{STANDARD_1}} | {{IMPACT_1}} | {{PRIORITE_1}} | {{MIGRATION_1}} |
| {{DOMAINE_2}} | {{ECART_2}} | {{STANDARD_2}} | {{IMPACT_2}} | {{PRIORITE_2}} | {{MIGRATION_2}} |

## 10. Risques associes

Risques critiques :

- {{RISQUE_CRITIQUE_1}}
- {{RISQUE_CRITIQUE_2}}

Risques importants :

- {{RISQUE_IMPORTANT_1}}
- {{RISQUE_IMPORTANT_2}}

Risques opportunistes :

- {{RISQUE_OPPORTUNISTE_1}}
- {{RISQUE_OPPORTUNISTE_2}}

## 11. Trajectoire de migration par phases

Phase 0 - {{PHASE_0_NOM}} :

- {{PHASE_0_ACTION_1}}
- {{PHASE_0_ACTION_2}}

Phase 1 - {{PHASE_1_NOM}} :

- {{PHASE_1_ACTION_1}}
- {{PHASE_1_ACTION_2}}

Phase 2 - {{PHASE_2_NOM}} :

- {{PHASE_2_ACTION_1}}
- {{PHASE_2_ACTION_2}}

## 12. Decisions a faire trancher par le proprietaire

| Decision | Options | Recommandation audit |
|---|---|---|
| {{DECISION_1}} | {{OPTIONS_1}} | {{RECOMMANDATION_1}} |
| {{DECISION_2}} | {{OPTIONS_2}} | {{RECOMMANDATION_2}} |

## 13. Quick wins possibles

- {{QUICK_WIN_1}}
- {{QUICK_WIN_2}}
- {{QUICK_WIN_3}}

## 14. Points a ne pas modifier sans validation

- {{ZONE_PROTEGEE_1}}
- {{ZONE_PROTEGEE_2}}
- {{ZONE_PROTEGEE_3}}

## 15. Incertitudes notees

- {{INCERTITUDE_1}}
- {{INCERTITUDE_2}}
