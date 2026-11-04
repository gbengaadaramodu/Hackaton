import WelcomeBanner from '../../components/dashboard/WelcomeBanner';
import EnrolledClasses from '../../components/dashboard/EnrolledClasses';
import AssignmentsWidget from '../../components/dashboard/AssignmentsWidget';
import RightWidgets from '../../components/dashboard/RightWidgets';

export default function Dashboard() {
  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* 1. Welcome Banner */}
      <WelcomeBanner />

      {/* 2. Main Responsive Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Columns */}
        <div className="lg:col-span-2 space-y-6">
          <EnrolledClasses />
          <AssignmentsWidget />
        </div>

        {/* Right Widget Column */}
        <div className="space-y-6">
          <RightWidgets />
        </div>
      </div>
    </div>
  );
}
