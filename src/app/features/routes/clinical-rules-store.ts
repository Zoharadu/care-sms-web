import { Injectable } from '@angular/core';

import { ClinicalRuleAggregate } from './route.models';

function clone<T>(value: T): T {
  return structuredClone(value);
}

@Injectable({ providedIn: 'root' })
export class ClinicalRulesStore {
  private aggregates: ClinicalRuleAggregate[] = [];
  private templateId = 0;

  replaceAll(aggregates: ClinicalRuleAggregate[]): void {
    this.aggregates = clone(aggregates);
    this.templateId = this.aggregates[0]?.template.templateId ?? 0;
  }

  getAll(): ClinicalRuleAggregate[] {
    return this.aggregates;
  }

  findById(id: number): ClinicalRuleAggregate | undefined {
    return this.aggregates.find((aggregate) => aggregate.id === id);
  }

  findByTemplateId(templateId: number): ClinicalRuleAggregate | undefined {
    return this.aggregates.find((aggregate) => aggregate.template.templateId === templateId);
  }

  add(aggregate: ClinicalRuleAggregate): void {
    this.aggregates = [aggregate, ...this.aggregates];
  }

  remove(id: number): void {
    this.aggregates = this.aggregates.filter((aggregate) => aggregate.id !== id);
  }

  get selectedTemplateId(): number {
    return this.templateId;
  }

  set selectedTemplateId(templateId: number) {
    this.templateId = templateId;
  }
}
