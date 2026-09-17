import { ClinicalRuleAggregate } from '../routes/route.models';
import { PatientContext } from './template.models';

export function renderTemplate(rule: ClinicalRuleAggregate, patient: PatientContext): string {
  const language =
    rule.languages.find((entry) => entry.languageCode === patient.languageCode && entry.isActive) ??
    rule.languages.find((entry) => entry.isDefault && entry.isActive) ??
    rule.languages[0];

  return renderTemplateText(language.templateText, patient);
}

export function renderTemplateText(templateText: string, patient: PatientContext): string {
  const values: Record<string, string> = {
    '{PatientName}': patient.patientName,
    '{WardName}': patient.wardName,
    '{DepartmentName}': patient.wardName,
    '{HospitalName}': patient.hospitalName,
    '{DoctorName}': patient.doctorName,
    '{HeadNurseName}': 'נועה כהן',
    '{LinkToPresentation}': 'https://example.org/presentation',
    '{LinkPresentation}': 'https://example.org/info',
    '{kod_mita}': '12A',
    '{kod_cheder}': '304',
    '{AvgWaitNurse}': '8 דקות',
    '{AvgWaitDoctor}': '35 דקות',
    '{DoctorVisitHours}': '09:00-11:00',
    '{OptOutLink}': 'https://example.org/optout',
    '{DischargeDate}': new Intl.DateTimeFormat('he-IL').format(new Date(patient.simulatedAt)),
  };

  return templateText.replace(/\{[^{}]+\}/g, (token) => values[token] ?? token);
}
