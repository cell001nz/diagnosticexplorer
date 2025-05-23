import React from 'react';
import { Link } from 'react-router-dom';

interface BreadcrumbPath {
  name: string;
  url?: string;
};

// Define the props type
type BreadcrumbProps = {
  paths: BreadcrumbPath[];
};

const Breadcrumb: React.FC<BreadcrumbProps> = ({ paths }) => {
  return (
    <div className="text-md text-gray-400 mb-4">
      {paths.map((path, index) => (
        <React.Fragment key={index}>
          {path.url ? (
            <Link to={path.url} className="hover:text-gray-800">{path.name}</Link>
          ) : (
            <span>{path.name}</span>
          )}
          {index !== paths.length - 1 && <span className="mx-2">{'>'}</span>}
        </React.Fragment>
      ))}
    </div>
  );
};

export default Breadcrumb;