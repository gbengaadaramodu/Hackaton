// ─── Auth ──────────────────────────────────────────────────────────────────
export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  status: string;
  institutionId: string | null;
  institutionName: string | null;
  guardianEmail: string | null;
  guardianConsentAt: string | null;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: UserProfile;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  role: string;
  dateOfBirth?: string;
  guardianEmail?: string;
  guardianConsent?: boolean;
}

// ─── Classes ───────────────────────────────────────────────────────────────
export interface ClassInfo {
  id: string;
  name: string;
  joinCode: string;
  teacherId: string;
  teacherName: string;
  memberCount: number;
  institutionId?: string | null;
  createdAt: string;
}

export interface ClassMember {
  studentId: string;
  displayName: string;
  email: string;
  joinedAt: string;
}

export interface JoinClassRequest {
  joinCode: string;
}

export interface CreateClassRequest {
  name: string;
  institutionId?: string;
}

// ─── Assignments ───────────────────────────────────────────────────────────
export interface Assignment {
  id: string;
  classId: string;
  className?: string;
  lessonId: string;
  lessonTitle?: string;
  modelKey?: string;
  instructions?: string | null;
  dueAt?: string | null;
  teacherId: string;
  createdAt: string;
  completed?: boolean;
  lastSessionId?: string | null;
  bestAssessmentPercentage?: number | null;
}

export interface CreateAssignmentRequest {
  classId: string;
  lessonId: string;
  dueAt?: string;
  instructions?: string;
}

// ─── Lessons ───────────────────────────────────────────────────────────────
export interface LessonObjective {
  id?: string;
  code: string;
  description?: string;
  level?: string;
  subjectCode?: string;
}

export interface Lesson {
  id: string;
  title: string;
  description?: string | null;
  subjectCode: string;
  subjectName?: string;
  gradeLevel?: string;
  modelKey?: string;
  status: string;
  draftVersion?: number;
  publishedVersion?: number;
  publishedAt?: string | null;
  objectives?: Array<LessonObjective | string>;
  createdAt?: string;
  updatedAt?: string;
}

// ─── Sessions ──────────────────────────────────────────────────────────────
export type SessionStatus = 'Active' | 'Paused' | 'Completed';

export interface SimulationModelSummary {
  modelKey: string;
  title: string;
  subject: string;
  domain: string;
  schemaVersion: string;
  learningObjective: string;
  learningObjectiveCode: string;
  governingRelationship: string;
  renderer: string;
}

export interface SimulationObservation {
  label?: string;
  value?: string | number;
  detail?: string;
}

export interface SimulationResult {
  model?: SimulationModelSummary;
  state?: Record<string, number>;
  observations?: Array<string | SimulationObservation>;
  trajectory?: Array<Record<string, number>>;
  summary?: string;
  isInMotion?: boolean;
  warnings?: string[];
}

export interface SimulationSession {
  id: string;
  modelKey: string;
  model?: SimulationModelSummary;
  status: SessionStatus;
  version: number;
  state: Record<string, number>;
  initialState?: Record<string, number>;
  result?: SimulationResult;
  lessonId?: string | null;
  assignmentId?: string | null;
  startedAt: string;
  lastActivityAt: string;
  endedAt?: string | null;
  // Present on the session list projection
  modelTitle?: string;
  lessonTitle?: string;
  commandCount?: number;
  messageCount?: number;
}

export interface StartSessionRequest {
  modelKey?: string;
  lessonId?: string;
  assignmentId?: string;
  simulationRequestId?: string;
  initialState?: Record<string, number>;
}

export interface CommandRequest {
  command: {
    type: string;
    parameter?: string;
    value?: number;
  };
  expectedVersion?: number;
  reason?: string;
  text?: string;
}

export interface MessageRequest {
  message: string;
}

export interface ChatMessage {
  id: string;
  role: string;
  content: string;
  isAiGenerated?: boolean;
  provider?: string | null;
  createdAt: string;
}

// ─── Assessments ───────────────────────────────────────────────────────────
export type QuestionType = 'SingleChoice' | 'MultipleChoice' | 'Numeric';

export interface AssessmentOption {
  id: string;
  text: string;
}

export interface AssessmentQuestion {
  id: string;
  prompt: string;
  type: QuestionType;
  options: AssessmentOption[];
  learningObjectiveCode?: string | null;
}

export interface Assessment {
  id: string;
  title: string;
  modelKey?: string | null;
  lessonId?: string | null;
  version: number;
  questions: AssessmentQuestion[];
  // Present on the assessment list projection
  questionCount?: number;
}

export interface AssessmentAnswer {
  questionId: string;
  selectedOptionIds?: string[];
  numericValue?: number | null;
}

export interface SubmitAttemptRequest {
  answers: AssessmentAnswer[];
  sessionId?: string;
}

export interface QuestionFeedback {
  questionId: string;
  correct: boolean;
  correctAnswer: string;
  submittedAnswer?: string | null;
  explanation: string;
  points: number;
}

export interface AttemptResult {
  attemptId: string;
  assessmentId: string;
  assessmentVersion: number;
  score: number;
  maxScore: number;
  percentage: number;
  passed: boolean;
  feedback: QuestionFeedback[];
  submittedAt: string;
}

export interface AssessmentAttempt {
  attemptId: string;
  assessmentId: string;
  assessmentTitle: string;
  assessmentVersion: number;
  percentage: number;
  passed: boolean;
  submittedAt: string;
}

// ─── Catalog ───────────────────────────────────────────────────────────────
export interface SimulationParameter {
  name: string;
  label: string;
  min: number;
  max: number;
  step: number;
  unit?: string;
  default?: number;
  defaultValue?: number;
  symbol?: string;
  description?: string;
  hasIntegralValuesOnly?: boolean;
}

export interface SimulationTemplate {
  modelKey: string;
  title: string;
  description?: string;
  subject: string;
  domain?: string;
  schemaVersion?: string;
  learningObjective?: string;
  learningObjectiveCode?: string;
  governingRelationship?: string;
  renderer?: string;
  parameters?: SimulationParameter[];
  concepts?: Array<Record<string, string>>;
  features?: string[];
  assumptions?: string[];
  limitations?: string[];
}

export interface Subject {
  id?: string;
  code: string;
  name: string;
  description?: string;
}

export interface CatalogResponse {
  subjects?: Subject[];
  objectives?: Array<Record<string, string>>;
  simulations?: SimulationTemplate[];
}

// ─── Reports ───────────────────────────────────────────────────────────────
export interface ObjectiveMastery {
  code: string;
  description?: string;
  attempts: number;
  correctAnswers: number;
  masteryRate?: number | null;
}

export interface StudentReport {
  userId: string;
  displayName: string;
  sessionsStarted: number;
  sessionsCompleted: number;
  attempts: number;
  averagePercentage?: number | null;
  recentAttempts?: AssessmentAttempt[];
  objectives?: ObjectiveMastery[];
  generatedAt?: string;
}
