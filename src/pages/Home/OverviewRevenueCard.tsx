import React from 'react';


// Define the props type
type RevenueCardProps = {
    title: string;
    revenue: number;
};

const RevenueCard: React.FC<RevenueCardProps> = ({ title, revenue }) => {
    return (
      <div className="border rounded p-4">
        <h2 className="text-lg font-semibold mb-2">{title}</h2>
        <p className="text-xl">${revenue}</p>
      </div>
    );
  };
  
  export default RevenueCard;