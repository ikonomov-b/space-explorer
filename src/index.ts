export type DistanceTier = "short" | "medium" | "long";

export interface DestinationPreferences {
  distanceTier: DistanceTier;
  environmentBias: string[];
  surveyFocus: string[];
}

export interface Artifact {
  id: string;
  name: string;
  value: number;
  origin: string;
}

export interface Destination {
  id: string;
  seed: string;
  preferences: DestinationPreferences;
  artifacts: Artifact[];
}

export interface CampaignState {
  campaignId: string;
  discoveredDestinations: Destination[];
}

export function createEmptyCampaign(campaignId: string): CampaignState {
  return {
    campaignId,
    discoveredDestinations: [],
  };
}
